using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using ThreadBeacon.Core.AutoRecovery;

namespace ThreadBeacon.App.AutoRecovery;

public sealed class WindowsCodexComposerAutomation : ICodexComposerAutomation
{
    private const string ComposerClassPrefix = "ProseMirror";
    private const string TrailingBreakClass = "ProseMirror-trailingBreak";
    private static readonly TimeSpan NavigationTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan UiTimeout = TimeSpan.FromSeconds(3);

    private readonly Dictionary<string, SessionState> sessions = new(StringComparer.Ordinal);

    public Task<CodexComposerSession?> SelectEmptyTargetAsync(
        string threadId,
        string expectedTitle,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        if (!Guid.TryParse(threadId, out _) || string.IsNullOrWhiteSpace(expectedTitle))
        {
            return null;
        }

        IntPtr windowHandle = FindUniqueCodexWindow();
        if (windowHandle == IntPtr.Zero || NativeMethods.GetForegroundWindow() == windowHandle)
        {
            return null;
        }

        AutomationElement root = AutomationElement.FromHandle(windowHandle);
        ComposerState? preflight = TryGetUniqueComposer(root);
        if (preflight is not { IsEmptyPlaceholder: true })
        {
            return null;
        }

        int[] previousRuntimeId = preflight.Element.GetRuntimeId();
        Process.Start(new ProcessStartInfo($"codex://threads/{threadId}")
        {
            UseShellExecute = true,
        });

        AutomationElement? target = WaitForTarget(
            windowHandle,
            expectedTitle,
            previousRuntimeId,
            cancellationToken);
        if (target is null || ReadComposer(target) is not { IsEmptyPlaceholder: true })
        {
            return null;
        }

        string sessionId = Guid.NewGuid().ToString("N");
        lock (sessions)
        {
            sessions.Clear();
            sessions[sessionId] = new SessionState(windowHandle, target);
        }

        return new CodexComposerSession(sessionId);
    }, cancellationToken);

    public Task<bool> FocusAsync(
        CodexComposerSession session,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        if (!TryGetSession(session, out SessionState? state)
            || !NativeMethods.SetForegroundWindow(state.WindowHandle))
        {
            return false;
        }

        state.Composer.SetFocus();
        return WaitUntil(
            () => state.Composer.Current.HasKeyboardFocus,
            UiTimeout,
            cancellationToken);
    }, cancellationToken);

    public Task TypeAsync(
        CodexComposerSession session,
        string text,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryGetSession(session, out SessionState? state)
            || !state.Composer.Current.HasKeyboardFocus)
        {
            throw new InvalidOperationException("Codex composer is not focused.");
        }

        NativeMethods.TypeUnicode(text);
    }, cancellationToken);

    public Task<string> ReadTextAsync(
        CodexComposerSession session,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return TryGetSession(session, out SessionState? state)
            ? ReadText(state.Composer)
            : throw new InvalidOperationException("Codex composer session is unavailable.");
    }, cancellationToken);

    public Task<bool> CanInvokeUniqueSendButtonAsync(
        CodexComposerSession session,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryGetSession(session, out SessionState? state))
        {
            return false;
        }

        AutomationElement root = AutomationElement.FromHandle(state.WindowHandle);
        AutomationElement[] buttons = root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
            .Cast<AutomationElement>()
            .ToArray();
        System.Windows.Rect composerBounds = state.Composer.Current.BoundingRectangle;
        AccessibilityButtonCandidate[] candidates = buttons
            .Select(button => ToCandidate(button, composerBounds))
            .ToArray();
        int? selected = AccessibilitySendButtonPolicy.Select(candidates);
        state.SendButton = selected is int index ? buttons[index] : null;
        return state.SendButton is not null;
    }, cancellationToken);

    public Task InvokeSendAsync(
        CodexComposerSession session,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryGetSession(session, out SessionState? state)
            || state.SendButton is null
            || !state.SendButton.TryGetCurrentPattern(InvokePattern.Pattern, out object? pattern))
        {
            throw new InvalidOperationException("Codex send button is unavailable.");
        }

        ((InvokePattern)pattern).Invoke();
    }, cancellationToken);

    public Task<bool> ClearIfTextEqualsAsync(
        CodexComposerSession session,
        string expectedText,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        if (!TryGetSession(session, out SessionState? state)
            || !SubmittedTextMatches(ReadText(state.Composer), expectedText))
        {
            return false;
        }

        state.Composer.SetFocus();
        if (!WaitUntil(
                () => state.Composer.Current.HasKeyboardFocus,
                UiTimeout,
                cancellationToken))
        {
            return false;
        }

        NativeMethods.ClearFocusedText();
        return WaitUntil(
            () => ReadComposer(state.Composer).IsEmptyPlaceholder,
            UiTimeout,
            cancellationToken);
    }, cancellationToken);

    private static IntPtr FindUniqueCodexWindow()
    {
        IntPtr[] handles = Process.GetProcessesByName("ChatGPT")
            .Select(TryGetCodexWindowHandle)
            .Where(handle => handle != IntPtr.Zero)
            .Distinct()
            .ToArray();
        return handles.Length == 1 ? handles[0] : IntPtr.Zero;
    }

    private static IntPtr TryGetCodexWindowHandle(Process process)
    {
        try
        {
            IntPtr handle = process.MainWindowHandle;
            return handle != IntPtr.Zero
                && process.MainModule?.FileName.Contains(
                    "OpenAI.Codex_",
                    StringComparison.OrdinalIgnoreCase) == true
                ? handle
                : IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
        finally
        {
            process.Dispose();
        }
    }

    private static AutomationElement? WaitForTarget(
        IntPtr windowHandle,
        string expectedTitle,
        int[] previousRuntimeId,
        CancellationToken cancellationToken)
    {
        AutomationElement? result = null;
        WaitUntil(() =>
        {
            try
            {
                AutomationElement root = AutomationElement.FromHandle(windowHandle);
                AutomationElement[] titles = root.FindAll(
                        TreeScope.Descendants,
                        new AndCondition(
                            new PropertyCondition(
                                AutomationElement.ControlTypeProperty,
                                ControlType.Text),
                            new PropertyCondition(
                                AutomationElement.NameProperty,
                                expectedTitle)))
                    .Cast<AutomationElement>()
                    .Where(IsAppHeaderTitle)
                    .ToArray();
                ComposerState? composer = TryGetUniqueComposer(root);
                if (titles.Length != 1
                    || composer is null
                    || composer.Element.GetRuntimeId().SequenceEqual(previousRuntimeId))
                {
                    return false;
                }

                result = composer.Element;
                return true;
            }
            catch (ElementNotAvailableException)
            {
                return false;
            }
        }, NavigationTimeout, cancellationToken);
        return result;
    }

    private static bool IsAppHeaderTitle(AutomationElement element)
    {
        AutomationElement? current = element;
        for (int depth = 0; depth < 6 && current is not null; depth++)
        {
            current = TreeWalker.ControlViewWalker.GetParent(current);
            if (current?.Current.ClassName.Contains(
                    "app-header-tint",
                    StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static ComposerState? TryGetUniqueComposer(AutomationElement root)
    {
        AutomationElement[] composers = root.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Group))
            .Cast<AutomationElement>()
            .Where(element => element.Current.ClassName.StartsWith(
                ComposerClassPrefix,
                StringComparison.Ordinal))
            .ToArray();
        return composers.Length == 1 ? ReadComposer(composers[0]) : null;
    }

    private static ComposerState ReadComposer(AutomationElement composer)
    {
        string text = ReadText(composer).Replace("\r", string.Empty).Replace("\n", string.Empty);
        bool hasTrailingBreak = composer.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.ClassNameProperty,
                    TrailingBreakClass)).Count == 1;
        return new ComposerState(composer, hasTrailingBreak && text.Length > 0);
    }

    private static string ReadText(AutomationElement element)
    {
        if (!element.TryGetCurrentPattern(TextPattern.Pattern, out object? pattern))
        {
            throw new InvalidOperationException("Codex composer does not expose text content.");
        }

        return ((TextPattern)pattern).DocumentRange.GetText(-1);
    }

    private static AccessibilityButtonCandidate ToCandidate(
        AutomationElement button,
        System.Windows.Rect composerBounds)
    {
        System.Windows.Rect bounds = button.Current.BoundingRectangle;
        bool isNear = !bounds.IsEmpty
            && bounds.Left >= composerBounds.Left - 20
            && bounds.Right <= composerBounds.Right + 20
            && bounds.Top >= composerBounds.Top - 40
            && bounds.Bottom <= composerBounds.Bottom + 140;
        return new AccessibilityButtonCandidate(
            button.Current.Name,
            button.Current.AutomationId,
            button.Current.IsEnabled,
            button.TryGetCurrentPattern(InvokePattern.Pattern, out _),
            isNear,
            button.Current.ClassName);
    }

    private static bool SubmittedTextMatches(string actual, string expected)
    {
        if (!actual.StartsWith(expected, StringComparison.Ordinal))
        {
            return false;
        }

        ReadOnlySpan<char> suffix = actual.AsSpan(expected.Length);
        return suffix.Length <= 2 && suffix.IndexOfAnyExcept('\r', '\n') < 0;
    }

    private bool TryGetSession(
        CodexComposerSession session,
        [NotNullWhen(true)] out SessionState? state)
    {
        lock (sessions)
        {
            return sessions.TryGetValue(session.Id, out state);
        }
    }

    private static bool WaitUntil(
        Func<bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (predicate())
            {
                return true;
            }

            Thread.Sleep(100);
        }

        return false;
    }

    private sealed record ComposerState(
        AutomationElement Element,
        bool IsEmptyPlaceholder);

    private sealed class SessionState(
        IntPtr windowHandle,
        AutomationElement composer)
    {
        public IntPtr WindowHandle { get; } = windowHandle;

        public AutomationElement Composer { get; } = composer;

        public AutomationElement? SendButton { get; set; }
    }

    private static class NativeMethods
    {
        private const uint InputKeyboard = 1;
        private const uint KeyeventfKeyup = 0x0002;
        private const uint KeyeventfUnicode = 0x0004;
        private const ushort VkControl = 0x11;
        private const ushort VkA = 0x41;
        private const ushort VkBack = 0x08;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr windowHandle);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint inputCount, Input[] inputs, int size);

        internal static void TypeUnicode(string value)
        {
            List<Input> inputs = [];
            foreach (char character in value)
            {
                inputs.Add(Key(character, KeyeventfUnicode));
                inputs.Add(Key(character, KeyeventfUnicode | KeyeventfKeyup));
            }

            Send(inputs.ToArray());
        }

        internal static void ClearFocusedText() => Send([
            Key(VkControl, 0),
            Key(VkA, 0),
            Key(VkA, KeyeventfKeyup),
            Key(VkControl, KeyeventfKeyup),
            Key(VkBack, 0),
            Key(VkBack, KeyeventfKeyup),
        ]);

        private static Input Key(ushort code, uint flags) => new()
        {
            Type = InputKeyboard,
            Union = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = (flags & KeyeventfUnicode) != 0 ? (ushort)0 : code,
                    ScanCode = (flags & KeyeventfUnicode) != 0 ? code : (ushort)0,
                    Flags = flags,
                },
            },
        };

        private static void Send(Input[] inputs)
        {
            if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) != inputs.Length)
            {
                throw new InvalidOperationException("Windows did not accept all keyboard input.");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            internal uint Type;
            internal InputUnion Union;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] internal KeyboardInput Keyboard;
            [FieldOffset(0)] internal MouseInput Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            internal ushort VirtualKey;
            internal ushort ScanCode;
            internal uint Flags;
            internal uint Time;
            internal IntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            internal int X;
            internal int Y;
            internal uint MouseData;
            internal uint Flags;
            internal uint Time;
            internal IntPtr ExtraInfo;
        }
    }
}
