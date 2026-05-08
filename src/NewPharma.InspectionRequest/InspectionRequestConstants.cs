namespace NewPharma.InspectionRequest;

internal static class InspectionRequestConstants
{
    public const string EntityName = "NPH_INSPECTION_REQUEST";

    public const string FieldRequestId = "REQUEST_ID";
    public const string FieldIdText = "ID_TEXT";
    public const string FieldStatus = "STATUS";
    public const string FieldLoginPlanId = "LOGIN_PLAN_ID";
    public const string FieldLoginPlanVersion = "LOGIN_PLAN_VERSION";
    public const string FieldUseLastActiveVersion = "USE_LAST_ACTIVE_VERSION";
    public const string FieldRootContextTable = "ROOT_CONTEXT_TABLE";
    public const string FieldRootContextId = "ROOT_CONTEXT_ID";
    public const string FieldExecutionStatus = "EXECUTION_STATUS";
    public const string FieldExecutionStartedOn = "EXECUTION_STARTED_ON";
    public const string FieldExecutionCompletedOn = "EXECUTION_COMPLETED_ON";
    public const string FieldGeneratedJobId = "GENERATED_JOB_ID";
    public const string FieldGeneratedObjectSummary = "GENERATED_OBJECT_SUMMARY";
    public const string FieldExecutionError = "EXECUTION_ERROR";
    public const string FieldEsigRequired = "ESIG_REQUIRED";

    public const string StatusDraft = "Draft";
    public const string StatusSubmitted = "Submitted";
    public const string StatusUnderReview = "UnderReview";
    public const string StatusApproved = "Approved";
    public const string StatusExecuting = "Executing";
    public const string StatusExecuted = "Executed";
    public const string StatusExecutionFailed = "ExecutionFailed";

    public const string ExecutionNotExecuted = "NotExecuted";
    public const string ExecutionExecuting = "Executing";
    public const string ExecutionExecuted = "Executed";
    public const string ExecutionFailed = "Failed";
}
