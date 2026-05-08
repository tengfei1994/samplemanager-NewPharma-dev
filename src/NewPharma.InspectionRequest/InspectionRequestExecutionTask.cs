using System;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Tasks;

namespace NewPharma.InspectionRequest;

/// <summary>
/// Workflow/task entry point for executing an approved Inspection Request.
/// This wrapper is intentionally separate from the vendor Login Plan source.
/// </summary>
[SampleManagerTask("NewPharmaInspectionRequestExecutionTask")]
public class InspectionRequestExecutionTask : SampleManagerTask
{
    protected override void SetupTask()
    {
        base.SetupTask();

        IEntity request = Context?.SelectedItems?.Count > 0
            ? Context.SelectedItems[0]
            : null;

        if (request == null)
        {
            Library.Utils.FlashMessage("No Inspection Request was selected.", "Inspection Request");
            Exit(false);
            return;
        }

        try
        {
            var service = new InspectionRequestExecutionService(Library, EntityManager);
            service.Execute(request);
            Exit(true);
        }
        catch (Exception ex)
        {
            Library.Utils.FlashMessage(ex.Message, "Inspection Request Execution Failed");
            Exit(false);
        }
    }
}
