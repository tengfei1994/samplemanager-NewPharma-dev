namespace NewPharma.InspectionRequest
{
    internal static class InspectionRequestConstants
    {
        public const string EntityName = "NPH_INSPECTION_REQUEST";

        public const string FieldRequestId = "REQUEST_ID";
        public const string FieldIdText = "ID_TEXT";
        public const string FieldStatus = "STATUS";
        public const string FieldLoginPlan = "LoginPlan";
        public const string FieldLoginPlanId = "LOGIN_PLAN_ID";
        public const string FieldLoginPlanIdProperty = "LoginPlanId";
        public const string FieldLoginPlanVersion = "LOGIN_PLAN_VERSION";
        public const string FieldLoginPlanVersionProperty = "LoginPlanVersion";
        public const string FieldUseLastActiveVersion = "USE_LAST_ACTIVE_VERSION";
        public const string FieldRootContextTable = "ROOT_CONTEXT_TABLE";
        public const string FieldRootContextId = "ROOT_CONTEXT_ID";
        public const string FieldRequestedBy = "REQUESTED_BY";
        public const string FieldRequestedOn = "REQUESTED_ON";
        public const string FieldExecutionStatus = "EXECUTION_STATUS";
        public const string FieldExecutionStartedOn = "EXECUTION_STARTED_ON";
        public const string FieldExecutionCompletedOn = "EXECUTION_COMPLETED_ON";
        public const string FieldGeneratedJobId = "GENERATED_JOB_ID";
        public const string FieldGeneratedObjectSummary = "GENERATED_OBJECT_SUMMARY";
        public const string FieldExecutionError = "EXECUTION_ERROR";
        public const string FieldEsigRequired = "ESIG_REQUIRED";
        public const string FieldLifecycleWorkflowId = "LIFECYCLE_WORKFLOW_ID";
        public const string FieldLifecycleWorkflowVersion = "LIFECYCLE_WORKFLOW_VERSION";
        public const string FieldLifecycleNodeId = "LIFECYCLE_NODE_ID";
        public const string FieldLifecycleEvent = "LIFECYCLE_EVENT";
        public const string FieldEntityTemplateId = "ENTITY_TEMPLATE_ID";
        public const string FieldEntityTemplateVersion = "ENTITY_TEMPLATE_VERSION";

        public const string TableLoginPlanEntry = "LOGIN_PLAN_ENTRY";
        public const string TableLoginPlanField = "LOGIN_PLAN_FIELD";
        public const string TableLoginPlanTest = "LOGIN_PLAN_TEST";
        public const string TableLoginPlanTestField = "LOGIN_PLAN_TEST_FIELD";
        public const string TableLoginPlan = "LOGIN_PLAN";
        public const string TableMlpHeader = "MLP_HEADER";

        public const string TableIrLoginPlanEntry = "NPH_IR_LP_ENTRY";
        public const string TableIrLoginPlanField = "NPH_IR_LP_FIELD";
        public const string TableIrLoginPlanTest = "NPH_IR_LP_TEST";
        public const string TableIrLoginPlanTestField = "NPH_IR_LP_TEST_FIELD";
        public const string TableIrProduct = "NPH_IR_PRODUCT";
        public const string TableWorkflow = "WORKFLOW";
        public const string TableWorkflowNode = "WORKFLOW_NODE";
        public const string TableWorkflowLink = "WORKFLOW_LINK";
        public const string TableWorkflowJournal = "WORKFLOW_JOURNAL";
        public const string TableEntityTemplate = "ENTITY_TEMPLATE";

        public const string StatusDraft = "DRAFT";
        public const string StatusSubmitted = "SUBMITTED";
        public const string StatusUnderReview = "UNDER_REVI";
        public const string StatusApproved = "APPROVED";
        public const string StatusExecuting = "EXECUTING";
        public const string StatusExecuted = "EXECUTED";
        public const string StatusExecutionFailed = "EXECUTION_";

        public const string ExecutionNotExecuted = "NOT_STARTE";
        public const string ExecutionExecuting = "EXECUTING";
        public const string ExecutionExecuted = "EXECUTED";
        public const string ExecutionFailed = "FAILED";
    }
}
