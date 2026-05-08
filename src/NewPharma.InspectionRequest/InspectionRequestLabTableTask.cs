using Thermo.SampleManager.Library;
using Thermo.SampleManager.Tasks;

namespace NewPharma.InspectionRequest;

/// <summary>
/// Standard LabTable maintenance entry for the NewPharma Inspection Request table.
/// </summary>
[SampleManagerTask("NewPharmaInspectionRequestTask", "LABTABLE", InspectionRequestConstants.EntityName)]
public class InspectionRequestLabTableTask : GenericLabtableTask
{
}
