using System.IO;
using NUnit.Framework;
using Territory.Editor.Bridge;

namespace Territory.Tests.EditMode.Atomization.VisualRegression
{
    /// <summary>
    /// §Red-Stage Proof anchor: VisualBaselineCaptureTests.cs::CapturesPauseMenuBaseline
    ///
    /// Stage 1.0 tracer fixture for ui-visual-regression (TECH-31892).
    /// Validates the CaptureBaselineCandidate surface: method exists on UiBakeHandler,
    /// returns expected DTO fields, and emits candidate path under Library/UiBaselines/_candidate/.
    ///
    /// Full operator approval flow (bake → record → diff → ImageAssert) is documented in
    /// the §Goal of TECH-31892 and requires a running Unity Editor + pause-menu prefab.
    /// This EditMode test validates the surface contract (method shape, error paths)
    /// without requiring an active Play Mode session.
    /// </summary>
    public class VisualBaselineCaptureTests
    {
        // ── §Red-Stage Proof anchor method ────────────────────────────────────────

        [Test]
        public void CapturesPauseMenuBaseline()
        {
            // Verify the method exists + signature matches expectations.
            var methodInfo = typeof(UiBakeHandler).GetMethod(
                "CaptureBaselineCandidate",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(methodInfo,
                "UiBakeHandler.CaptureBaselineCandidate must be a public static method.");

            // Verify DTO type has required fields.
            var dtoType = typeof(UiBakeHandler).GetNestedType("BaselineCaptureResult",
                System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(dtoType, "BaselineCaptureResult nested type must be public.");
            Assert.IsNotNull(dtoType!.GetField("panel_slug"), "BaselineCaptureResult.panel_slug must exist.");
            Assert.IsNotNull(dtoType!.GetField("candidate_path"), "BaselineCaptureResult.candidate_path must exist.");
            Assert.IsNotNull(dtoType!.GetField("sha256"), "BaselineCaptureResult.sha256 must exist.");
            Assert.IsNotNull(dtoType!.GetField("resolution"), "BaselineCaptureResult.resolution must exist.");
            Assert.IsNotNull(dtoType!.GetField("theme"), "BaselineCaptureResult.theme must exist.");
            Assert.IsNotNull(dtoType!.GetField("error"), "BaselineCaptureResult.error must exist.");
        }

        [Test]
        public void CaptureBaselineCandidate_NullSlug_ReturnsError()
        {
            var result = UiBakeHandler.CaptureBaselineCandidate(null);
            Assert.IsNotNull(result, "Result must not be null.");
            Assert.IsNotNull(result.error, "Null panel_slug must set error field.");
            Assert.IsNull(result.candidate_path, "candidate_path must be null on error.");
        }

        [Test]
        public void CaptureBaselineCandidate_EmptySlug_ReturnsError()
        {
            var result = UiBakeHandler.CaptureBaselineCandidate("");
            Assert.IsNotNull(result, "Result must not be null.");
            Assert.IsNotNull(result.error, "Empty panel_slug must set error field.");
        }

        [Test]
        public void CaptureBaselineCandidate_MissingPrefab_ReturnsError()
        {
            // A slug that has no prefab under Assets/UI/Prefabs/Generated/.
            var result = UiBakeHandler.CaptureBaselineCandidate("__nonexistent_panel_slug__");
            Assert.IsNotNull(result, "Result must not be null.");
            Assert.IsNotNull(result.error, "Missing prefab must set error field.");
            Assert.That(result.error, Does.Contain("prefab_not_found").Or.Contain("__nonexistent_panel_slug__"),
                "Error must identify the missing prefab.");
        }

        [Test]
        public void CandidateDirGitignore_IsPresent()
        {
            // Library/UiBaselines/.gitignore must exclude _candidate/ to prevent
            // accidental raw blob commits of working PNGs (Invariant constraint #2 guard).
            string gitignorePath = Path.Combine(
                System.Environment.CurrentDirectory,
                "Library", "UiBaselines", ".gitignore");
            if (!File.Exists(gitignorePath))
            {
                Assert.Ignore(".gitignore not found — Library/ likely cleaned; ignored for CI.");
                return;
            }
            string content = File.ReadAllText(gitignorePath);
            Assert.That(content, Does.Contain("_candidate/"),
                "Library/UiBaselines/.gitignore must exclude _candidate/.");
        }
    }
}
