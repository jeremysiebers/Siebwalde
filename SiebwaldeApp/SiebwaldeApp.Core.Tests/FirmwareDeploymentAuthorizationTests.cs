using Xunit;

namespace SiebwaldeApp.Core.Tests;

public class FirmwareDeploymentAuthorizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static FirmwareArtifactIdentity Artifact(string path = "fw.hex", ushort checksum = 0xE906)
        => new(path, new string('A', 64), checksum);

    private static FirmwareDeploymentSession ActiveSession(
        bool authorized = true,
        params DeploymentTarget[] targets)
        => new("s1", authorized, targets, Artifact(), Now.AddHours(1));

    [Fact]
    public void Authorize_NullSession_Denied()
    {
        var decision = DeploymentAuthorizer.Authorize(
            null, DeploymentTarget.ForAmplifier(1), artifactVerified: true, nowUtc: Now);
        Assert.False(decision.Granted);
        Assert.Equal(DeploymentAuthorizer.NoActiveSession, decision.DenialReason);
    }

    [Fact]
    public void Authorize_NotAuthorizedSession_Denied()
    {
        var session = ActiveSession(authorized: false, DeploymentTarget.ForAmplifier(1));
        var decision = DeploymentAuthorizer.Authorize(session, DeploymentTarget.ForAmplifier(1), true, Now);
        Assert.False(decision.Granted);
        Assert.Equal(DeploymentAuthorizer.SessionNotAuthorized, decision.DenialReason);
    }

    [Fact]
    public void Authorize_ExpiredSession_Denied()
    {
        var session = new FirmwareDeploymentSession(
            "s1", true, new[] { DeploymentTarget.ForAmplifier(1) }, Artifact(), Now.AddSeconds(-1));
        var decision = DeploymentAuthorizer.Authorize(session, DeploymentTarget.ForAmplifier(1), true, Now);
        Assert.False(decision.Granted);
        Assert.Equal(DeploymentAuthorizer.SessionExpired, decision.DenialReason);
    }

    [Fact]
    public void Authorize_WrongTarget_Denied()
    {
        var session = ActiveSession(targets: DeploymentTarget.ForAmplifier(1));
        var decision = DeploymentAuthorizer.Authorize(session, DeploymentTarget.ForAmplifier(2), true, Now);
        Assert.False(decision.Granted);
        Assert.Equal(DeploymentAuthorizer.TargetOutOfScope, decision.DenialReason);
    }

    [Fact]
    public void Authorize_UnverifiedArtifact_Denied()
    {
        var session = ActiveSession(targets: DeploymentTarget.ForAmplifier(1));
        var decision = DeploymentAuthorizer.Authorize(session, DeploymentTarget.ForAmplifier(1), false, Now);
        Assert.False(decision.Granted);
        Assert.Equal(DeploymentAuthorizer.ArtifactNotVerified, decision.DenialReason);
    }

    [Fact]
    public void Authorize_ValidSessionTargetAndArtifact_Allow()
    {
        var session = ActiveSession(targets: DeploymentTarget.ForAmplifier(1));
        var decision = DeploymentAuthorizer.Authorize(session, DeploymentTarget.ForAmplifier(1), true, Now);
        Assert.True(decision.Granted);
        Assert.Null(decision.DenialReason);
    }

    [Fact]
    public void Session_RejectsFleetOfAmplifiers()
    {
        var ex = Assert.Throws<ArgumentException>(() => new FirmwareDeploymentSession(
            "s1",
            true,
            new[] { DeploymentTarget.ForAmplifier(1), DeploymentTarget.ForAmplifier(2) },
            Artifact(),
            Now.AddHours(1)));
        Assert.Contains("at most", ex.Message);
    }

    [Fact]
    public void Session_IsActiveHonorsExpiryAndAuthorization()
    {
        var active = new FirmwareDeploymentSession(
            "s1", true, new[] { DeploymentTarget.ForAmplifier(1) }, Artifact(), Now.AddHours(1));
        Assert.True(active.IsActive(Now));
        Assert.False(active.IsActive(Now.AddHours(2)));

        var unauthorized = new FirmwareDeploymentSession(
            "s2", false, new[] { DeploymentTarget.ForAmplifier(1) }, Artifact(), Now.AddHours(1));
        Assert.False(unauthorized.IsActive(Now));
    }

    [Fact]
    public void EventLog_RecordsDeploymentAndRollbackEvents()
    {
        var log = new DeploymentEventLog();
        var target = DeploymentTarget.ForAmplifier(3);
        var when = new DateTimeOffset(2026, 10, 18, 1, 2, 3, TimeSpan.Zero);

        log.Record(new DeploymentEvent(DeploymentEventKind.Authorized, target, "HASH", "ok", when));
        log.RecordRollback(new RollbackEvent(target, "HASH", "blocked", when));

        var deploymentEvent = Assert.Single(log.DeploymentEvents);
        Assert.Equal(DeploymentEventKind.Authorized, deploymentEvent.Kind);
        Assert.Equal(target, deploymentEvent.Target);
        Assert.Equal("HASH", deploymentEvent.ArtifactSha256Hex);
        Assert.Equal(when, deploymentEvent.TimestampUtc);

        var rollbackEvent = Assert.Single(log.RollbackEvents);
        Assert.Equal("blocked", rollbackEvent.Result);
        Assert.Equal(target, rollbackEvent.Target);
    }
}
