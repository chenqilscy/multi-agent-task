using CKY.MultiAgentFramework.Core.Models.Dialog;
using Xunit;

namespace CKY.MultiAgentFramework.Tests.UnitTests.Core.Models.Dialog
{
    public class ClarificationModelsTests
    {
        [Fact]
        public void ClarificationAnalysis_CanBeInstantiated()
        {
            // Arrange & Act
            var analysis = new ClarificationAnalysis
            {
                NeedsClarification = true,
                Strategy = ClarificationStrategy.Template,
                Confidence = 0.8,
                EstimatedTurns = 2,
                RequiresConfirmation = false
            };

            // Assert
            Assert.True(analysis.NeedsClarification);
            Assert.Equal(ClarificationStrategy.Template, analysis.Strategy);
            Assert.Equal(0.8, analysis.Confidence);
            Assert.Equal(2, analysis.EstimatedTurns);
        }

        [Fact]
        public void ClarificationContext_CanBeInstantiated()
        {
            // Arrange & Act
            var context = new ClarificationContext
            {
                SessionId = "test-session",
                Intent = "control_device",
                TurnCount = 1,
                Strategy = ClarificationStrategy.Template,
                IsCompleted = false
            };

            // Assert
            Assert.Equal("test-session", context.SessionId);
            Assert.Equal("control_device", context.Intent);
            Assert.Equal(1, context.TurnCount);
            Assert.False(context.IsCompleted);
        }

        [Fact]
        public void ClarificationResponse_CanBeInstantiated()
        {
            // Arrange & Act
            var response = new ClarificationResponse
            {
                Completed = false,
                NeedsFurtherClarification = true,
                Message = "Please provide more information"
            };

            // Assert
            Assert.False(response.Completed);
            Assert.True(response.NeedsFurtherClarification);
            Assert.Equal("Please provide more information", response.Message);
        }

        [Fact]
        public void ClarificationStrategy_HasAllRequiredValues()
        {
            // Assert
            Assert.Equal(4, Enum.GetValues<ClarificationStrategy>().Length);
            Assert.Contains(ClarificationStrategy.Template, Enum.GetValues<ClarificationStrategy>());
            Assert.Contains(ClarificationStrategy.SmartInference, Enum.GetValues<ClarificationStrategy>());
            Assert.Contains(ClarificationStrategy.LLM, Enum.GetValues<ClarificationStrategy>());
            Assert.Contains(ClarificationStrategy.Hybrid, Enum.GetValues<ClarificationStrategy>());
        }
    }
}
