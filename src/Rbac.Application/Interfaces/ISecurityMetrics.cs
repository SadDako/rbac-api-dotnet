namespace Rbac.Application.Interfaces;

public interface ISecurityMetrics
{
    void SuspiciousLogin();
    void BruteForceAttempt();
    void TokenReuse();
    void ImpossibleTravel();
    void SuspiciousDevice();
    void CompromisedSession();
    void AdaptiveMfa();
}
