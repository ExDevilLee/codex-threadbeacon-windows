namespace ThreadBeacon.App.Tests.Release;

public sealed class RepositoryPublicReadinessTests
{
    private static string RepositoryRoot => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", ".."));

    [Fact]
    public void Repository_ProvidesPublicMaintenanceAndTroubleshootingDocuments()
    {
        string[] paths =
        [
            "CHANGELOG.md",
            "CONTRIBUTING.md",
            "SECURITY.md",
            "docs/troubleshooting.md",
            "docs/troubleshooting-en.md",
            ".github/ISSUE_TEMPLATE/bug_report.yml",
            ".github/ISSUE_TEMPLATE/feature_request.yml",
            ".github/ISSUE_TEMPLATE/config.yml",
            ".github/pull_request_template.md",
        ];

        Assert.All(paths, path => Assert.True(
            File.Exists(Path.Combine(RepositoryRoot, path)),
            $"Missing public repository document: {path}"));
    }

    [Fact]
    public void IssueTemplates_TargetWindowsRepositoryAndForbidSensitiveCodexData()
    {
        string issueDirectory = Path.Combine(RepositoryRoot, ".github", "ISSUE_TEMPLATE");
        string content = string.Join('\n', Directory.GetFiles(issueDirectory)
            .Select(File.ReadAllText));

        Assert.Contains("codex-threadbeacon-windows", content, StringComparison.Ordinal);
        Assert.DoesNotContain("codex-threadbeacon-macos", content, StringComparison.Ordinal);
        Assert.Contains("state_5.sqlite", content, StringComparison.Ordinal);
        Assert.Contains("凭据", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Troubleshooting_UsesWindowsPathsAndInstallLocation()
    {
        string chinese = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "docs",
            "troubleshooting.md"));
        string english = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "docs",
            "troubleshooting-en.md"));

        Assert.Contains(@"%USERPROFILE%\.codex", chinese, StringComparison.Ordinal);
        Assert.Contains(@"%LOCALAPPDATA%\ThreadBeacon", chinese, StringComparison.Ordinal);
        Assert.Contains(@"%USERPROFILE%\.codex", english, StringComparison.Ordinal);
        Assert.DoesNotContain("/Applications", chinese, StringComparison.Ordinal);
        Assert.DoesNotContain("Gatekeeper", english, StringComparison.Ordinal);
    }

    [Fact]
    public void SponsorPage_ProvidesOptionalPaymentQrCodesWithoutFeatureUnlocks()
    {
        string sponsor = File.ReadAllText(Path.Combine(RepositoryRoot, "SPONSOR.md"));
        string[] imagePaths =
        [
            "docs/assets/sponsor/wechat-pay.jpg",
            "docs/assets/sponsor/alipay.jpg",
        ];

        foreach (string imagePath in imagePaths)
        {
            string absolutePath = Path.Combine(
                RepositoryRoot,
                imagePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(absolutePath), $"Missing sponsor QR image: {imagePath}");
            byte[] header = File.ReadAllBytes(absolutePath)[..3];
            Assert.Equal(new byte[] { 0xFF, 0xD8, 0xFF }, header);
            Assert.Contains(imagePath, sponsor, StringComparison.Ordinal);
        }

        Assert.Contains("完全自愿", sponsor, StringComparison.Ordinal);
        Assert.Contains("不解锁功能", sponsor, StringComparison.Ordinal);
        Assert.Contains("不在 App 主窗口展示付款广告或二维码", sponsor, StringComparison.Ordinal);
        Assert.Contains("unlocks no features", sponsor, StringComparison.Ordinal);
        Assert.Contains("stay out of the main app window", sponsor, StringComparison.Ordinal);
    }
}
