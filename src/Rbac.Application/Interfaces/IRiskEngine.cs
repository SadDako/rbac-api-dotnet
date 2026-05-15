using Rbac.Application.Security;

namespace Rbac.Application.Interfaces;

public interface IRiskEngine
{
    RiskAssessment Evaluate(RiskEvaluationContext context);
}
