using System;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Tasks;

namespace NewPharma.InspectionRequest
{
    [SampleManagerTask("ConfigureInspectionRequestLifecycleTask")]
    public class ConfigureInspectionRequestLifecycleTask : SampleManagerTask
    {
        protected override void SetupTask()
        {
            new InspectionRequestLifecycleService(EntityManager).EnsureConfiguration();
            Library.Utils.FlashMessage("Inspection Request lifecycle configuration has been created or updated.", "NewPharma");
            Exit(true);
        }
    }

    public abstract class InspectionRequestLifecycleActionTask : SampleManagerTask
    {
        protected abstract string Action { get; }

        protected override void SetupTask()
        {
            try
            {
                IEntity request = Context?.SelectedItems?.Count > 0
                    ? Context.SelectedItems[0] as IEntity
                    : null;

                if (request == null || !request.IsValid())
                {
                    throw new InvalidOperationException("Select one Inspection Request before running this lifecycle action.");
                }

                var lifecycleService = new InspectionRequestLifecycleService(EntityManager);
                lifecycleService.Initialize(request);
                lifecycleService.Move(request, Action);
                Library.Utils.FlashMessage($"Inspection Request lifecycle action completed: {Action}.", "Inspection Request");
                Exit(true);
            }
            catch (Exception ex)
            {
                Library.Utils.FlashMessage(ex.Message, "Inspection Request Lifecycle");
                Exit(false);
            }
        }
    }

    [SampleManagerTask("NewPharmaInspectionRequestSubmitTask")]
    public sealed class InspectionRequestSubmitTask : InspectionRequestLifecycleActionTask
    {
        protected override string Action => "SUBMIT";
    }

    [SampleManagerTask("NewPharmaInspectionRequestReviewTask")]
    public sealed class InspectionRequestReviewTask : InspectionRequestLifecycleActionTask
    {
        protected override string Action => "REVIEW";
    }

    [SampleManagerTask("NewPharmaInspectionRequestApproveTask")]
    public sealed class InspectionRequestApproveTask : InspectionRequestLifecycleActionTask
    {
        protected override string Action => "APPROVE";
    }

    [SampleManagerTask("NewPharmaInspectionRequestRejectTask")]
    public sealed class InspectionRequestRejectTask : InspectionRequestLifecycleActionTask
    {
        protected override string Action => "REJECT";
    }
}
