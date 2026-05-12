SET NOCOUNT ON;

IF COL_LENGTH('NPH_INSPECTION_REQUEST', 'NAME') IS NULL
    ALTER TABLE NPH_INSPECTION_REQUEST ADD NAME nvarchar(100) NOT NULL CONSTRAINT DF_NPH_IR_NAME DEFAULT '';

IF COL_LENGTH('NPH_INSPECTION_REQUEST', 'DESCRIPTION') IS NULL
    ALTER TABLE NPH_INSPECTION_REQUEST ADD DESCRIPTION nvarchar(2000) NOT NULL CONSTRAINT DF_NPH_IR_DESCRIPTION DEFAULT '';

IF COL_LENGTH('NPH_INSPECTION_REQUEST', 'APPROVAL_REQD') IS NULL
    ALTER TABLE NPH_INSPECTION_REQUEST ADD APPROVAL_REQD nvarchar(1) NOT NULL CONSTRAINT DF_NPH_IR_APPROVAL_REQD DEFAULT 'F';

IF COL_LENGTH('NPH_INSPECTION_REQUEST', 'APPROVAL_STATUS') IS NULL
    ALTER TABLE NPH_INSPECTION_REQUEST ADD APPROVAL_STATUS nvarchar(1) NOT NULL CONSTRAINT DF_NPH_IR_APPROVAL_STATUS DEFAULT 'A';

IF COL_LENGTH('NPH_INSPECTION_REQUEST', 'INSPECTION_PLAN') IS NULL
    ALTER TABLE NPH_INSPECTION_REQUEST ADD INSPECTION_PLAN nvarchar(10) NOT NULL CONSTRAINT DF_NPH_IR_INSPECTION_PLAN DEFAULT '';

IF EXISTS (
    SELECT 1
    FROM SCHEMA_TABLE_FIELD
    WHERE TABLE_ID = 'NPH_INSPECTION_REQUEST'
      AND [IDENTITY] = 'HAS_ATTACHMENTS'
      AND ISNULL(TRY_CONVERT(int, LTRIM(RTRIM(ORDER_NUMBER))), 0) <> 35
)
    UPDATE SCHEMA_TABLE_FIELD
    SET ORDER_NUMBER = '        35',
        MODIFIED_ON = GETDATE()
    WHERE TABLE_ID = 'NPH_INSPECTION_REQUEST'
      AND [IDENTITY] = 'HAS_ATTACHMENTS';

IF NOT EXISTS (SELECT 1 FROM SCHEMA_TABLE_FIELD WHERE TABLE_ID = 'NPH_INSPECTION_REQUEST' AND [IDENTITY] = 'DESCRIPTION')
BEGIN
    INSERT INTO SCHEMA_TABLE_FIELD
    (
        SCHEMA_GUID, SCHEMA_VERSION, TABLE_ID, [IDENTITY], ORDER_NUMBER, MODIFIED_ON, MODIFIED_BY, MODIFIABLE,
        FIELD_TYPE, COMMENTS, ALIASES, TEXTUAL_LENGTH, DESCENDING,
        LINK_TABLE_GUID, LINK_TABLE_VERSION, LINK_TABLE_ID, LINK_FIELD_ID, LINK_NAME,
        PROMPT_DESCRIPTION, READ_ONLY, DEFAULT_FIELD_VALUE,
        PHRASE_TYPE, PHRASE_IDENTITIES, PHRASE_DISPLAY_DESCRIPTION, PROMPT_TYPE, PHRASE_IS_CHOOSE, PHRASE_IS_VALID, PHRASE_NO_IDENTITIES,
        PROMPT_LIBRARY, PROMPT_ROUTINE, TRUE_WORD, FALSE_WORD, ALLOWED_CHARS, UPPER_LIMIT, LOWER_LIMIT,
        OVERRIDE, FORMAT, CASE_SENSITIVE, IS_GUID, IS_KEY, VALID_REFERENCE, LINKS_TO_PARENT,
        IS_MODIFIABLE_FIELD, IS_MODIFIED_ON_FIELD, IS_MODIFIED_BY_FIELD, IS_GROUP_FIELD, IS_ORDER_FIELD, IS_VERSION_FIELD,
        IS_APPROVAL_STATUS_FIELD, IS_INSPECTION_FIELD, IS_BROWSE_FIELD, BROWSE_ORDER, IS_PRIMARY_BROWSE,
        IS_ACTIVE_FLAG_FIELD, IS_ACTIVE_START_DATE_FIELD, IS_ACTIVE_END_DATE_FIELD, IS_NAME_FIELD, IS_REMOVE_FLAG,
        IS_HAS_ATTACHMENTS_FIELD, IS_HAS_NOTEBOOK_LINK_FIELD, AUDIT_MODE, IS_ASSIGNABLE_FLAG, IS_FULL_DESCRIPTION,
        IS_TRANSLATABLE, IS_TRANSLATION, RENAME_FIELD
    )
    SELECT
        SCHEMA_GUID,
        SCHEMA_VERSION,
        TABLE_ID,
        'DESCRIPTION',
        '        36',
        GETDATE(),
        MODIFIED_BY,
        'T',
        'TEXT',
        '',
        '',
        2000,
        DESCENDING,
        '',
        '',
        '',
        '',
        '',
        'Description',
        'F',
        '',
        '',
        'F',
        'F',
        '',
        'F',
        'F',
        'F',
        '',
        '',
        TRUE_WORD,
        FALSE_WORD,
        '',
        '',
        '',
        'F',
        '',
        CASE_SENSITIVE,
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        0,
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        'F',
        AUDIT_MODE,
        'F',
        'F',
        'F',
        'F',
        ''
    FROM SCHEMA_TABLE_FIELD
    WHERE TABLE_ID = 'NPH_INSPECTION_REQUEST'
      AND [IDENTITY] = 'NAME';
END
ELSE
BEGIN
    UPDATE SCHEMA_TABLE_FIELD
    SET FIELD_TYPE = 'TEXT',
        TEXTUAL_LENGTH = 2000,
        PROMPT_DESCRIPTION = 'Description',
        IS_NAME_FIELD = 'F',
        IS_FULL_DESCRIPTION = 'F',
        IS_KEY = 'F',
        IS_BROWSE_FIELD = 'F',
        IS_PRIMARY_BROWSE = 'F',
        ORDER_NUMBER = '        36',
        MODIFIABLE = 'T',
        READ_ONLY = 'F',
        MODIFIED_ON = GETDATE()
    WHERE TABLE_ID = 'NPH_INSPECTION_REQUEST'
      AND [IDENTITY] = 'DESCRIPTION';
END

DECLARE @IrInspectionField TABLE
(
    [IDENTITY] nvarchar(40) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    ORDER_NUMBER varchar(10) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    FIELD_TYPE nvarchar(20) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    TEXTUAL_LENGTH int NOT NULL,
    LINK_TABLE_ID nvarchar(40) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    LINK_FIELD_ID nvarchar(40) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    PROMPT_DESCRIPTION nvarchar(100) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    DEFAULT_FIELD_VALUE nvarchar(255) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    PHRASE_TYPE nvarchar(20) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    IS_APPROVAL_STATUS_FIELD varchar(1) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    IS_INSPECTION_FIELD varchar(1) COLLATE Latin1_General_CI_AS_WS NOT NULL
);

INSERT INTO @IrInspectionField
    ([IDENTITY], ORDER_NUMBER, FIELD_TYPE, TEXTUAL_LENGTH, LINK_TABLE_ID, LINK_FIELD_ID,
     PROMPT_DESCRIPTION, DEFAULT_FIELD_VALUE, PHRASE_TYPE, IS_APPROVAL_STATUS_FIELD, IS_INSPECTION_FIELD)
VALUES
    ('APPROVAL_REQD', '        37', 'BOOL', 1, '', '', 'Approval Required', 'F', '', 'F', 'F'),
    ('APPROVAL_STATUS', '        38', 'TEXT', 1, '', '', 'Approval Status', 'A', 'APPR_STAT', 'T', 'F'),
    ('INSPECTION_PLAN', '        39', 'LINK', 10, 'INSPECTION_HEADER', 'IDENTITY', 'Inspection Plan', '', '', 'F', 'T');

MERGE SCHEMA_TABLE_FIELD AS target
USING (
    SELECT
        base.SCHEMA_GUID,
        base.SCHEMA_VERSION,
        base.TABLE_ID,
        defs.[IDENTITY],
        defs.ORDER_NUMBER,
        defs.FIELD_TYPE,
        defs.TEXTUAL_LENGTH,
        defs.LINK_TABLE_ID,
        defs.LINK_FIELD_ID,
        defs.PROMPT_DESCRIPTION,
        defs.DEFAULT_FIELD_VALUE,
        defs.PHRASE_TYPE,
        defs.IS_APPROVAL_STATUS_FIELD,
        defs.IS_INSPECTION_FIELD,
        base.MODIFIED_BY,
        base.DESCENDING,
        base.TRUE_WORD,
        base.FALSE_WORD,
        base.CASE_SENSITIVE,
        base.AUDIT_MODE
    FROM @IrInspectionField defs
    CROSS JOIN SCHEMA_TABLE_FIELD base
    WHERE base.TABLE_ID = 'NPH_INSPECTION_REQUEST'
      AND base.[IDENTITY] = 'NAME'
) AS source
ON target.TABLE_ID = source.TABLE_ID
   AND target.[IDENTITY] = source.[IDENTITY]
WHEN MATCHED THEN
    UPDATE SET ORDER_NUMBER = source.ORDER_NUMBER,
               FIELD_TYPE = source.FIELD_TYPE,
               TEXTUAL_LENGTH = source.TEXTUAL_LENGTH,
               LINK_TABLE_GUID = '',
               LINK_TABLE_VERSION = '',
               LINK_TABLE_ID = source.LINK_TABLE_ID,
               LINK_FIELD_ID = source.LINK_FIELD_ID,
               LINK_NAME = '',
               PROMPT_DESCRIPTION = source.PROMPT_DESCRIPTION,
               READ_ONLY = 'F',
               DEFAULT_FIELD_VALUE = source.DEFAULT_FIELD_VALUE,
               PHRASE_TYPE = source.PHRASE_TYPE,
               IS_APPROVAL_STATUS_FIELD = source.IS_APPROVAL_STATUS_FIELD,
               IS_INSPECTION_FIELD = source.IS_INSPECTION_FIELD,
               IS_NAME_FIELD = 'F',
               IS_KEY = 'F',
               IS_BROWSE_FIELD = 'F',
               IS_PRIMARY_BROWSE = 'F',
               MODIFIABLE = 'T',
               MODIFIED_ON = GETDATE()
WHEN NOT MATCHED THEN
    INSERT
    (
        SCHEMA_GUID, SCHEMA_VERSION, TABLE_ID, [IDENTITY], ORDER_NUMBER, MODIFIED_ON, MODIFIED_BY, MODIFIABLE,
        FIELD_TYPE, COMMENTS, ALIASES, TEXTUAL_LENGTH, DESCENDING,
        LINK_TABLE_GUID, LINK_TABLE_VERSION, LINK_TABLE_ID, LINK_FIELD_ID, LINK_NAME,
        PROMPT_DESCRIPTION, READ_ONLY, DEFAULT_FIELD_VALUE,
        PHRASE_TYPE, PHRASE_IDENTITIES, PHRASE_DISPLAY_DESCRIPTION, PROMPT_TYPE, PHRASE_IS_CHOOSE, PHRASE_IS_VALID, PHRASE_NO_IDENTITIES,
        PROMPT_LIBRARY, PROMPT_ROUTINE, TRUE_WORD, FALSE_WORD, ALLOWED_CHARS, UPPER_LIMIT, LOWER_LIMIT,
        OVERRIDE, FORMAT, CASE_SENSITIVE, IS_GUID, IS_KEY, VALID_REFERENCE, LINKS_TO_PARENT,
        IS_MODIFIABLE_FIELD, IS_MODIFIED_ON_FIELD, IS_MODIFIED_BY_FIELD, IS_GROUP_FIELD, IS_ORDER_FIELD, IS_VERSION_FIELD,
        IS_APPROVAL_STATUS_FIELD, IS_INSPECTION_FIELD, IS_BROWSE_FIELD, BROWSE_ORDER, IS_PRIMARY_BROWSE,
        IS_ACTIVE_FLAG_FIELD, IS_ACTIVE_START_DATE_FIELD, IS_ACTIVE_END_DATE_FIELD, IS_NAME_FIELD, IS_REMOVE_FLAG,
        IS_HAS_ATTACHMENTS_FIELD, IS_HAS_NOTEBOOK_LINK_FIELD, AUDIT_MODE, IS_ASSIGNABLE_FLAG, IS_FULL_DESCRIPTION,
        IS_TRANSLATABLE, IS_TRANSLATION, RENAME_FIELD
    )
    VALUES
    (
        source.SCHEMA_GUID, source.SCHEMA_VERSION, source.TABLE_ID, source.[IDENTITY], source.ORDER_NUMBER, GETDATE(), source.MODIFIED_BY, 'T',
        source.FIELD_TYPE, '', '', source.TEXTUAL_LENGTH, source.DESCENDING,
        '', '', source.LINK_TABLE_ID, source.LINK_FIELD_ID, '',
        source.PROMPT_DESCRIPTION, 'F', source.DEFAULT_FIELD_VALUE,
        source.PHRASE_TYPE, 'F', 'F', '', 'F', 'F', 'F',
        '', '', source.TRUE_WORD, source.FALSE_WORD, '', '', '',
        'F', '', source.CASE_SENSITIVE, 'F', 'F', 'F', 'F',
        'F', 'F', 'F', 'F', 'F', 'F',
        source.IS_APPROVAL_STATUS_FIELD, source.IS_INSPECTION_FIELD, 'F', 0, 'F',
        'F', 'F', 'F', 'F', 'F',
        'F', 'F', source.AUDIT_MODE, 'F', 'F',
        'F', 'F', ''
    );

UPDATE SCHEMA_TABLE
SET MODIFIED_ON = GETDATE()
WHERE [IDENTITY] = 'NPH_INSPECTION_REQUEST';

IF COL_LENGTH('NPH_INSPECTION_REQUEST', 'WORKFLOW_NODE') IS NULL
    ALTER TABLE NPH_INSPECTION_REQUEST ADD WORKFLOW_NODE nvarchar(36) NOT NULL CONSTRAINT DF_NPH_IR_WORKFLOW_NODE DEFAULT '';

IF COL_LENGTH('NPH_INSPECTION_REQUEST', 'ENTITY_TEMPLATE_ID') IS NULL
    ALTER TABLE NPH_INSPECTION_REQUEST ADD ENTITY_TEMPLATE_ID nvarchar(30) NOT NULL CONSTRAINT DF_NPH_IR_ENTITY_TEMPLATE_ID DEFAULT '';

IF COL_LENGTH('NPH_INSPECTION_REQUEST', 'ENTITY_TEMPLATE_VERSION') IS NULL
    ALTER TABLE NPH_INSPECTION_REQUEST ADD ENTITY_TEMPLATE_VERSION varchar(10) NOT NULL CONSTRAINT DF_NPH_IR_ENTITY_TEMPLATE_VERSION DEFAULT '';

DECLARE @Entity nvarchar(30) = 'NPH_INSPECTION_REQUEST';
DECLARE @LifecycleWorkflow nvarchar(36) = '19940000-0000-0000-0000-000000000001';
DECLARE @LoginWorkflow nvarchar(36) = '19940000-0000-0000-0000-000000000002';
DECLARE @Version varchar(10) = '         1';
DECLARE @Template nvarchar(30) = 'NPH_IR';
DECLARE @LifecycleRoot nvarchar(36) = '19940000-0000-0000-0000-000000000100';
DECLARE @LoginNode nvarchar(36) = '19940000-0000-0000-0000-000000000201';
DECLARE @LoginCreateNode nvarchar(36) = '19940000-0000-0000-0000-000000000202';
DECLARE @WorkflowType nvarchar(30) = 'NPHIR';
DECLARE @WorkflowTypeText nvarchar(234) = 'Inspection Request Create';
DECLARE @EntityParams nvarchar(4000) = '<item><key><string>ENTITY_NAME</string></key><value><string>NPH_INSPECTION_REQUEST</string></value></item>';
DECLARE @LockParams nvarchar(4000) = '<item><key><string>ENTITY_NAME</string></key><value><string>NPH_INSPECTION_REQUEST</string></value><key><string>LOCK</string></key><value><string>T</string></value></item>';

MERGE PHRASE AS target
USING (
    SELECT 'WFLOW_TYPE' AS PHRASE_TYPE,
           @WorkflowType AS PHRASE_ID,
           '        19' AS ORDER_NUM,
           @WorkflowTypeText AS PHRASE_TEXT,
           'TEXT_TREE_PLAY' AS ICON
) AS source
ON target.PHRASE_TYPE = source.PHRASE_TYPE
   AND target.PHRASE_ID = source.PHRASE_ID
WHEN MATCHED THEN UPDATE SET
    ORDER_NUM = source.ORDER_NUM,
    PHRASE_TEXT = source.PHRASE_TEXT,
    ICON = source.ICON
WHEN NOT MATCHED THEN INSERT
    (PHRASE_TYPE, PHRASE_ID, ORDER_NUM, PHRASE_TEXT, ICON)
VALUES
    (source.PHRASE_TYPE, source.PHRASE_ID, source.ORDER_NUM, source.PHRASE_TEXT, source.ICON);

DELETE FROM WORKFLOW_NODE
WHERE WORKFLOW_ID IN (@LifecycleWorkflow, @LoginWorkflow)
  AND WORKFLOW_VERSION <> @Version;

DELETE FROM WORKFLOW
WHERE WORKFLOW_GUID IN (@LifecycleWorkflow, @LoginWorkflow)
  AND WORKFLOW_VERSION <> @Version;

IF NOT EXISTS (SELECT 1 FROM ENTITY_TEMPLATE WHERE [IDENTITY] = @Template AND ENTITY_TEMPLATE_VERSION = @Version)
BEGIN
    INSERT INTO ENTITY_TEMPLATE
        ([IDENTITY], ENTITY_TEMPLATE_VERSION, NAME, DESCRIPTION, TABLE_NAME, ACTIVE, APPROVAL_STATUS, MODIFIABLE, REMOVEFLAG)
    VALUES
        (@Template, @Version, 'NewPharma Inspection Request', 'Default entity template for NewPharma Inspection Request', @Entity, 'T', 'A', 'T', 'F');
END
ELSE
BEGIN
    UPDATE ENTITY_TEMPLATE
    SET NAME = 'NewPharma Inspection Request',
        DESCRIPTION = 'Default entity template for NewPharma Inspection Request',
        TABLE_NAME = @Entity,
        ACTIVE = 'T',
        APPROVAL_STATUS = 'A',
        MODIFIABLE = 'T',
        REMOVEFLAG = 'F'
    WHERE [IDENTITY] = @Template AND ENTITY_TEMPLATE_VERSION = @Version;
END

UPDATE NPH_INSPECTION_REQUEST
SET ENTITY_TEMPLATE_VERSION = @Version
WHERE ENTITY_TEMPLATE_ID = @Template
  AND ENTITY_TEMPLATE_VERSION <> @Version;

DELETE FROM ENTITY_TEMPLATE_PROPERTY
WHERE ENTITY_TEMPLATE_ID = @Template
  AND ENTITY_TEMPLATE_VERSION <> @Version;

DELETE FROM ENTITY_TEMPLATE
WHERE [IDENTITY] = @Template
  AND ENTITY_TEMPLATE_VERSION <> @Version;

DECLARE @IrTemplateProperty TABLE
(
    PROPERTY_NAME nvarchar(100) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    ORDER_NUM varchar(10) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    TITLE nvarchar(100) COLLATE Latin1_General_CI_AS_WS NOT NULL,
    DEFAULT_VALUE nvarchar(255) COLLATE Latin1_General_CI_AS_WS NOT NULL
);

INSERT INTO @IrTemplateProperty (PROPERTY_NAME, ORDER_NUM, TITLE, DEFAULT_VALUE)
VALUES
    ('Name', RIGHT('         ' + CAST(1 AS varchar(10)), 10), 'Name', ''),
    ('LoginPlan', RIGHT('         ' + CAST(2 AS varchar(10)), 10), 'Login Plan', ''),
    ('UseLastActiveVersion', RIGHT('         ' + CAST(3 AS varchar(10)), 10), 'Use Last Active Version', 'T'),
    ('EsigRequired', RIGHT('         ' + CAST(4 AS varchar(10)), 10), 'E-signature Required', 'F'),
    ('RootContextTable', RIGHT('         ' + CAST(5 AS varchar(10)), 10), 'Root Context Table', ''),
    ('RootContextId', RIGHT('         ' + CAST(6 AS varchar(10)), 10), 'Root Context ID', '');

EXEC(N'UPDATE NPH_INSPECTION_REQUEST
SET NAME = REQUEST_ID
WHERE LTRIM(RTRIM(ISNULL(NAME, ''''))) = ''''');

EXEC(N'UPDATE NPH_INSPECTION_REQUEST
SET DESCRIPTION = ''''
WHERE LTRIM(RTRIM(ISNULL(DESCRIPTION, ''''))) COLLATE DATABASE_DEFAULT = LTRIM(RTRIM(ISNULL(REQUEST_ID, ''''))) COLLATE DATABASE_DEFAULT');

MERGE ENTITY_TEMPLATE_PROPERTY AS target
USING @IrTemplateProperty AS source
ON target.PROPERTY_NAME = source.PROPERTY_NAME
   AND target.ENTITY_TEMPLATE_ID = @Template
   AND target.ENTITY_TEMPLATE_VERSION = @Version
WHEN MATCHED THEN
    UPDATE SET ORDER_NUM = source.ORDER_NUM,
               TITLE = source.TITLE,
               DEFAULT_VALUE = source.DEFAULT_VALUE,
               PROMPT_TYPE = 'PROMPT',
               CATEGORY = 'VALUE',
               DEFAULT_TYPE = '',
               CELL_WIDTH = '50',
               DISPLAY_RULE = '',
               PROPAGATE_VALUE = 'F'
WHEN NOT MATCHED THEN
    INSERT (PROPERTY_NAME, ENTITY_TEMPLATE_ID, ENTITY_TEMPLATE_VERSION, ORDER_NUM, TITLE, DEFAULT_VALUE,
            PROMPT_TYPE, FILTER_BY, CRITERIA, CATEGORY, DEFAULT_TYPE, CELL_WIDTH, DISPLAY_RULE, PROPAGATE_VALUE)
    VALUES (source.PROPERTY_NAME, @Template, @Version, source.ORDER_NUM, source.TITLE, source.DEFAULT_VALUE,
            'PROMPT', '', '', 'VALUE', '', '50', '', 'F');

DELETE FROM ENTITY_TEMPLATE_PROPERTY
WHERE ENTITY_TEMPLATE_ID = @Template
  AND ENTITY_TEMPLATE_VERSION = @Version
  AND PROPERTY_NAME IN ('IdText', 'Description');

IF NOT EXISTS (SELECT 1 FROM WORKFLOW WHERE WORKFLOW_GUID = @LifecycleWorkflow AND WORKFLOW_VERSION = @Version)
BEGIN
    INSERT INTO WORKFLOW
        (WORKFLOW_GUID, WORKFLOW_VERSION, NAME, TABLE_NAME, WORKFLOW_TYPE, DESCRIPTION, ACTIVE, MODIFIABLE, REMOVEFLAG)
    VALUES
        (@LifecycleWorkflow, @Version, 'NewPharma Inspection Request Lifecycle', @Entity, 'LIFECYCLE', 'Default lifecycle workflow for NewPharma Inspection Request', 'T', 'T', 'F');
END
ELSE
BEGIN
    UPDATE WORKFLOW
    SET NAME = 'NewPharma Inspection Request Lifecycle',
        TABLE_NAME = @Entity,
        WORKFLOW_TYPE = 'LIFECYCLE',
        DESCRIPTION = 'Default lifecycle workflow for NewPharma Inspection Request',
        ACTIVE = 'T',
        MODIFIABLE = 'T',
        REMOVEFLAG = 'F'
    WHERE WORKFLOW_GUID = @LifecycleWorkflow AND WORKFLOW_VERSION = @Version;
END

IF NOT EXISTS (SELECT 1 FROM WORKFLOW WHERE WORKFLOW_GUID = @LoginWorkflow AND WORKFLOW_VERSION = @Version)
BEGIN
    INSERT INTO WORKFLOW
        (WORKFLOW_GUID, WORKFLOW_VERSION, NAME, TABLE_NAME, WORKFLOW_TYPE, DESCRIPTION, ACTIVE, MODIFIABLE, REMOVEFLAG)
    VALUES
        (@LoginWorkflow, @Version, 'NewPharma Inspection Request Login', @Entity, @WorkflowType, 'Inspection Request create workflow entry point', 'T', 'T', 'F');
END
ELSE
BEGIN
    UPDATE WORKFLOW
    SET NAME = 'NewPharma Inspection Request Login',
        TABLE_NAME = @Entity,
        WORKFLOW_TYPE = @WorkflowType,
        DESCRIPTION = 'Inspection Request create workflow entry point',
        ACTIVE = 'T',
        MODIFIABLE = 'T',
        REMOVEFLAG = 'F'
    WHERE WORKFLOW_GUID = @LoginWorkflow AND WORKFLOW_VERSION = @Version;
END

IF NOT EXISTS (SELECT 1 FROM WORKFLOW_NODE WHERE WORKFLOW_NODE_GUID = @LifecycleRoot)
BEGIN
    INSERT INTO WORKFLOW_NODE
        (WORKFLOW_NODE_GUID, WORKFLOW_ID, WORKFLOW_VERSION, ORDER_NUMBER, PARENT_NODE, NAME, DESCRIPTION, NODE_TYPE, PARAMETERS_EXT, DEFAULT_WORKFLOW_ID, ENTITY_TEMPLATE_ID, ENABLED)
    VALUES
        (@LifecycleRoot, @LifecycleWorkflow, @Version, '1', '', 'Inspection Request Lifecycle', 'Default lifecycle for NewPharma Inspection Request', 'LIFECYCLE', @EntityParams, '', '', 'T');
END
ELSE
BEGIN
    UPDATE WORKFLOW_NODE
    SET WORKFLOW_ID = @LifecycleWorkflow,
        WORKFLOW_VERSION = @Version,
        ORDER_NUMBER = '1',
        PARENT_NODE = '',
        NAME = 'Inspection Request Lifecycle',
        DESCRIPTION = 'Default lifecycle for NewPharma Inspection Request',
        NODE_TYPE = 'LIFECYCLE',
        PARAMETERS_EXT = @EntityParams,
        DEFAULT_WORKFLOW_ID = '',
        ENTITY_TEMPLATE_ID = '',
        ENABLED = 'T'
    WHERE WORKFLOW_NODE_GUID = @LifecycleRoot;
END

IF NOT EXISTS (SELECT 1 FROM WORKFLOW_NODE WHERE WORKFLOW_NODE_GUID = @LoginNode)
BEGIN
    INSERT INTO WORKFLOW_NODE
        (WORKFLOW_NODE_GUID, WORKFLOW_ID, WORKFLOW_VERSION, ORDER_NUMBER, PARENT_NODE, NAME, DESCRIPTION, NODE_TYPE, PARAMETERS_EXT, DEFAULT_WORKFLOW_ID, ENTITY_TEMPLATE_ID, ENABLED)
    VALUES
        (@LoginNode, @LoginWorkflow, @Version, '1', '', 'Inspection Request Create Workflow', 'Inspection Request create workflow root',
         'NPHIR_LOGIN', '', '', '', 'T');
END
ELSE
BEGIN
    UPDATE WORKFLOW_NODE
    SET WORKFLOW_ID = @LoginWorkflow,
        WORKFLOW_VERSION = @Version,
        ORDER_NUMBER = '1',
        PARENT_NODE = '',
        NAME = 'Inspection Request Create Workflow',
        DESCRIPTION = 'Inspection Request create workflow root',
        NODE_TYPE = 'NPHIR_LOGIN',
        PARAMETERS_EXT = '',
        DEFAULT_WORKFLOW_ID = '',
        ACTION_TABLE_NAME = '',
        ACTION_TYPE_ID = '',
        STATE_TABLE_NAME = '',
        STATE_IDENTITY = '',
        EVENT_TABLE_NAME = '',
        EVENT_TYPE_ID = '',
        ENTITY_TEMPLATE_ID = '',
        ENABLED = 'T'
    WHERE WORKFLOW_NODE_GUID = @LoginNode;
END

IF NOT EXISTS (SELECT 1 FROM WORKFLOW_NODE WHERE WORKFLOW_NODE_GUID = @LoginCreateNode)
BEGIN
    INSERT INTO WORKFLOW_NODE
        (WORKFLOW_NODE_GUID, WORKFLOW_ID, WORKFLOW_VERSION, ORDER_NUMBER, PARENT_NODE, NAME, DESCRIPTION, NODE_TYPE, PARAMETERS_EXT, DEFAULT_WORKFLOW_ID, ENTITY_TEMPLATE_ID, ENABLED)
    VALUES
        (@LoginCreateNode, @LoginWorkflow, @Version, '2', @LoginNode, 'Create Inspection Request', 'Create Inspection Request',
         'CREATE_NPHIR', @EntityParams, @LifecycleWorkflow, @Template, 'T');
END
ELSE
BEGIN
    UPDATE WORKFLOW_NODE
    SET WORKFLOW_ID = @LoginWorkflow,
        WORKFLOW_VERSION = @Version,
        ORDER_NUMBER = '2',
        PARENT_NODE = @LoginNode,
        NAME = 'Create Inspection Request',
        DESCRIPTION = 'Create Inspection Request',
        NODE_TYPE = 'CREATE_NPHIR',
        PARAMETERS_EXT = @EntityParams,
        DEFAULT_WORKFLOW_ID = @LifecycleWorkflow,
        ACTION_TABLE_NAME = '',
        ACTION_TYPE_ID = '',
        STATE_TABLE_NAME = '',
        STATE_IDENTITY = '',
        EVENT_TABLE_NAME = '',
        EVENT_TYPE_ID = '',
        ENTITY_TEMPLATE_ID = @Template,
        ENABLED = 'T'
    WHERE WORKFLOW_NODE_GUID = @LoginCreateNode;
END

DELETE FROM WORKFLOW_NODE
WHERE WORKFLOW_ID = @LifecycleWorkflow
  AND WORKFLOW_VERSION = @Version
  AND NODE_TYPE = 'GENERAL';

DECLARE @Markers TABLE (
    NodeId nvarchar(36) COLLATE Latin1_General_CI_AS_WS,
    NodeName nvarchar(100) COLLATE Latin1_General_CI_AS_WS,
    OrderNo varchar(10) COLLATE Latin1_General_CI_AS_WS
);
INSERT INTO @Markers VALUES
('19940000-0000-0000-0000-000000000101', 'Draft', '101'),
('19940000-0000-0000-0000-000000000102', 'Submitted', '102'),
('19940000-0000-0000-0000-000000000103', 'Under Review', '103'),
('19940000-0000-0000-0000-000000000104', 'Approved', '104'),
('19940000-0000-0000-0000-000000000105', 'Executed', '105'),
('19940000-0000-0000-0000-000000000106', 'Executing', '106'),
('19940000-0000-0000-0000-000000000107', 'Execution Failed', '107');

MERGE WORKFLOW_NODE AS target
USING @Markers AS source
ON target.WORKFLOW_NODE_GUID = source.NodeId
WHEN MATCHED THEN UPDATE SET
    WORKFLOW_ID = @LifecycleWorkflow,
    WORKFLOW_VERSION = @Version,
    ORDER_NUMBER = source.OrderNo,
    PARENT_NODE = @LifecycleRoot,
    NAME = source.NodeName,
    DESCRIPTION = source.NodeName,
    NODE_TYPE = 'COMMENT',
    PARAMETERS_EXT = '',
    DEFAULT_WORKFLOW_ID = '',
    ACTION_TABLE_NAME = '',
    ACTION_TYPE_ID = '',
    STATE_TABLE_NAME = '',
    STATE_IDENTITY = '',
    EVENT_TABLE_NAME = '',
    EVENT_TYPE_ID = '',
    ENTITY_TEMPLATE_ID = '',
    ENABLED = 'T'
WHEN NOT MATCHED THEN INSERT
    (WORKFLOW_NODE_GUID, WORKFLOW_ID, WORKFLOW_VERSION, ORDER_NUMBER, PARENT_NODE, NAME, DESCRIPTION, NODE_TYPE, PARAMETERS_EXT, DEFAULT_WORKFLOW_ID, ENTITY_TEMPLATE_ID, ENABLED)
VALUES
    (source.NodeId, @LifecycleWorkflow, @Version, source.OrderNo, @LifecycleRoot, source.NodeName, source.NodeName, 'COMMENT', '', '', '', 'T');

DECLARE @Actions TABLE (
    ActionId nvarchar(30) COLLATE Latin1_General_CI_AS_WS,
    ActionName nvarchar(100) COLLATE Latin1_General_CI_AS_WS,
    StateId nvarchar(30) COLLATE Latin1_General_CI_AS_WS,
    TargetStatus nvarchar(30) COLLATE Latin1_General_CI_AS_WS,
    ActionNode nvarchar(36) COLLATE Latin1_General_CI_AS_WS,
    SetNode nvarchar(36) COLLATE Latin1_General_CI_AS_WS,
    OrderNo varchar(10) COLLATE Latin1_General_CI_AS_WS
);
INSERT INTO @Actions VALUES
('SUBMIT', 'Submit Inspection Request', 'DRAFT', 'SUBMITTED', '19940000-0000-0000-0000-000000000301', '19940000-0000-0000-0000-000000000302', '10'),
('REVIEW', 'Review Inspection Request', 'SUBMITTED', 'UNDER_REVI', '19940000-0000-0000-0000-000000000311', '19940000-0000-0000-0000-000000000312', '20'),
('APPROVE', 'Approve Inspection Request', 'UNDER_REVIEW', 'APPROVED', '19940000-0000-0000-0000-000000000321', '19940000-0000-0000-0000-000000000322', '30'),
('REJECT', 'Reject Inspection Request', 'REJECTABLE', 'DRAFT', '19940000-0000-0000-0000-000000000331', '19940000-0000-0000-0000-000000000332', '40');

MERGE WORKFLOW_ACTION_TYPE AS target
USING (SELECT ActionId, ActionName FROM @Actions) AS source
ON target.TABLE_NAME = @Entity AND target.[IDENTITY] = source.ActionId
WHEN MATCHED THEN UPDATE SET
    NAME = source.ActionName,
    DESCRIPTION = source.ActionName,
    INCLUDE_IN_MENU = 'T',
    ICON_ID = CASE source.ActionId
        WHEN 'SUBMIT' THEN 'INT_SUBMIT'
        WHEN 'REVIEW' THEN 'INT_VIEW'
        WHEN 'APPROVE' THEN 'INT_APPROVE'
        WHEN 'REJECT' THEN 'INT_REMOVE'
        ELSE target.ICON_ID
    END,
    MENU_TEXT = CASE source.ActionId
        WHEN 'SUBMIT' THEN 'Submit'
        WHEN 'REVIEW' THEN 'Review'
        WHEN 'APPROVE' THEN 'Approve'
        WHEN 'REJECT' THEN 'Reject'
        ELSE source.ActionName
    END,
    SUB_MENU_GROUP = 'Workflow',
    ACTION_TASK = CASE source.ActionId
        WHEN 'SUBMIT' THEN 'NewPharmaInspectionRequestSubmitTask'
        WHEN 'REVIEW' THEN 'NewPharmaInspectionRequestReviewTask'
        WHEN 'APPROVE' THEN 'NewPharmaInspectionRequestApproveTask'
        WHEN 'REJECT' THEN 'NewPharmaInspectionRequestRejectTask'
        ELSE target.ACTION_TASK
    END,
    MODIFIABLE = 'T',
    REMOVEFLAG = 'F'
WHEN NOT MATCHED THEN INSERT
    (TABLE_NAME, [IDENTITY], NAME, DESCRIPTION, INCLUDE_IN_MENU, ICON_ID, MENU_TEXT, SUB_MENU_GROUP, ACTION_TASK, MODIFIABLE, REMOVEFLAG)
VALUES
    (@Entity, source.ActionId, source.ActionName, source.ActionName, 'T',
     CASE source.ActionId WHEN 'SUBMIT' THEN 'INT_SUBMIT' WHEN 'REVIEW' THEN 'INT_VIEW' WHEN 'APPROVE' THEN 'INT_APPROVE' WHEN 'REJECT' THEN 'INT_REMOVE' ELSE '' END,
     CASE source.ActionId WHEN 'SUBMIT' THEN 'Submit' WHEN 'REVIEW' THEN 'Review' WHEN 'APPROVE' THEN 'Approve' WHEN 'REJECT' THEN 'Reject' ELSE source.ActionName END,
     'Workflow',
     CASE source.ActionId
        WHEN 'SUBMIT' THEN 'NewPharmaInspectionRequestSubmitTask'
        WHEN 'REVIEW' THEN 'NewPharmaInspectionRequestReviewTask'
        WHEN 'APPROVE' THEN 'NewPharmaInspectionRequestApproveTask'
        WHEN 'REJECT' THEN 'NewPharmaInspectionRequestRejectTask'
        ELSE ''
     END,
     'T', 'F');

DECLARE @Roles TABLE (ROLE_ID nvarchar(20) COLLATE Latin1_General_CI_AS_WS);
INSERT INTO @Roles VALUES ('USER'), ('STATIC_MAINTAINER'), ('DYNAMIC_MAINTAINER'), ('DYNAMIC_AUTHORISER');

MERGE WORKFLOW_ACTION_ROLE AS target
USING (
    SELECT @Entity COLLATE Latin1_General_CI_AS_WS AS WORKFLOW_ACTION_TYPE_TABLE, ActionId AS WORKFLOW_ACTION_TYPE_ID, ROLE_ID
    FROM @Actions CROSS JOIN @Roles
) AS source
ON target.WORKFLOW_ACTION_TYPE_TABLE = source.WORKFLOW_ACTION_TYPE_TABLE
   AND target.WORKFLOW_ACTION_TYPE_ID = source.WORKFLOW_ACTION_TYPE_ID
   AND target.ROLE_ID = source.ROLE_ID
WHEN NOT MATCHED THEN INSERT (WORKFLOW_ACTION_TYPE_TABLE, WORKFLOW_ACTION_TYPE_ID, ROLE_ID)
VALUES (source.WORKFLOW_ACTION_TYPE_TABLE, source.WORKFLOW_ACTION_TYPE_ID, source.ROLE_ID);

MERGE WORKFLOW_ROLE AS target
USING (
    SELECT @LoginWorkflow COLLATE Latin1_General_CI_AS_WS AS WORKFLOW_ID, @Version COLLATE Latin1_General_CI_AS_WS AS WORKFLOW_VERSION, ROLE_ID
    FROM @Roles
    UNION ALL
    SELECT @LifecycleWorkflow COLLATE Latin1_General_CI_AS_WS AS WORKFLOW_ID, @Version COLLATE Latin1_General_CI_AS_WS AS WORKFLOW_VERSION, ROLE_ID
    FROM @Roles
) AS source
ON target.WORKFLOW_ID = source.WORKFLOW_ID
   AND target.WORKFLOW_VERSION = source.WORKFLOW_VERSION
   AND target.ROLE_ID = source.ROLE_ID
WHEN NOT MATCHED THEN INSERT (WORKFLOW_ID, WORKFLOW_VERSION, ROLE_ID)
VALUES (source.WORKFLOW_ID, source.WORKFLOW_VERSION, source.ROLE_ID);

MERGE WORKFLOW_STATE AS target
USING (VALUES
    ('DRAFT', 'Draft', 'DRAFT'),
    ('SUBMITTED', 'Submitted', 'SUBMITTED'),
    ('UNDER_REVIEW', 'Under Review', 'UNDER_REVI'),
    ('APPROVED', 'Approved', 'APPROVED'),
    ('REJECTABLE', 'Rejectable', 'SUBMITTED,UNDER_REVI,APPROVED')
) AS source(StateId, StateName, StatusValue)
ON target.TABLE_NAME = @Entity AND target.[IDENTITY] = source.StateId
WHEN MATCHED THEN UPDATE SET
    NAME = source.StateName,
    DESCRIPTION = source.StateName,
    ANY_CONDITION_MATCH = CASE WHEN source.StateId = 'REJECTABLE' THEN 'T' ELSE 'F' END,
    TRACK = 'T',
    MODIFIABLE = 'T',
    REMOVEFLAG = 'F'
WHEN NOT MATCHED THEN INSERT
    (TABLE_NAME, [IDENTITY], NAME, DESCRIPTION, ANY_CONDITION_MATCH, TRACK, MODIFIABLE, REMOVEFLAG)
VALUES
    (@Entity, source.StateId, source.StateName, source.StateName, CASE WHEN source.StateId = 'REJECTABLE' THEN 'T' ELSE 'F' END, 'T', 'T', 'F');

DELETE FROM WORKFLOW_STATE_CONDITION WHERE TABLE_NAME = @Entity;
INSERT INTO WORKFLOW_STATE_CONDITION (TABLE_NAME, WORKFLOW_STATE_ID, ORDER_NUMBER, PROPERTY, OPERATOR, TYPE, VALUE) VALUES
(@Entity, 'DRAFT', '1', 'Status', '=', 'V', 'DRAFT'),
(@Entity, 'SUBMITTED', '1', 'Status', '=', 'V', 'SUBMITTED'),
(@Entity, 'UNDER_REVIEW', '1', 'Status', '=', 'V', 'UNDER_REVI'),
(@Entity, 'APPROVED', '1', 'Status', '=', 'V', 'APPROVED'),
(@Entity, 'REJECTABLE', '1', 'Status', '=', 'V', 'SUBMITTED'),
(@Entity, 'REJECTABLE', '2', 'Status', '=', 'V', 'UNDER_REVI'),
(@Entity, 'REJECTABLE', '3', 'Status', '=', 'V', 'APPROVED');

DECLARE @ActionId nvarchar(30),
        @ActionName nvarchar(100),
        @StateId nvarchar(30),
        @TargetStatus nvarchar(30),
        @ActionNode nvarchar(36),
        @SetNode nvarchar(36),
        @OrderNo varchar(10);
DECLARE action_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT ActionId, ActionName, StateId, TargetStatus, ActionNode, SetNode, OrderNo FROM @Actions;
OPEN action_cursor;
FETCH NEXT FROM action_cursor INTO @ActionId, @ActionName, @StateId, @TargetStatus, @ActionNode, @SetNode, @OrderNo;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM WORKFLOW_NODE WHERE WORKFLOW_NODE_GUID = @ActionNode)
    BEGIN
        INSERT INTO WORKFLOW_NODE
            (WORKFLOW_NODE_GUID, WORKFLOW_ID, WORKFLOW_VERSION, ORDER_NUMBER, PARENT_NODE, NAME, DESCRIPTION, NODE_TYPE, PARAMETERS_EXT, ACTION_TABLE_NAME, ACTION_TYPE_ID, STATE_TABLE_NAME, STATE_IDENTITY, DEFAULT_WORKFLOW_ID, ENTITY_TEMPLATE_ID, ENABLED)
        VALUES
            (@ActionNode, @LifecycleWorkflow, @Version, @OrderNo, @LifecycleRoot, @ActionName, @ActionName, 'ACTION_WORKFLOW', @LockParams, @Entity, @ActionId, @Entity, @StateId, '', '', 'T');
    END
    ELSE
    BEGIN
        UPDATE WORKFLOW_NODE
        SET WORKFLOW_ID = @LifecycleWorkflow,
            WORKFLOW_VERSION = @Version,
            ORDER_NUMBER = @OrderNo,
            PARENT_NODE = @LifecycleRoot,
            NAME = @ActionName,
            DESCRIPTION = @ActionName,
            NODE_TYPE = 'ACTION_WORKFLOW',
            PARAMETERS_EXT = @LockParams,
            ACTION_TABLE_NAME = @Entity,
            ACTION_TYPE_ID = @ActionId,
            STATE_TABLE_NAME = @Entity,
            STATE_IDENTITY = @StateId,
            DEFAULT_WORKFLOW_ID = '',
            ENTITY_TEMPLATE_ID = '',
            ENABLED = 'T'
        WHERE WORKFLOW_NODE_GUID = @ActionNode;
    END

    IF NOT EXISTS (SELECT 1 FROM WORKFLOW_NODE WHERE WORKFLOW_NODE_GUID = @SetNode)
    BEGIN
        INSERT INTO WORKFLOW_NODE
            (WORKFLOW_NODE_GUID, WORKFLOW_ID, WORKFLOW_VERSION, ORDER_NUMBER, PARENT_NODE, NAME, DESCRIPTION, NODE_TYPE, PARAMETERS_EXT, DEFAULT_WORKFLOW_ID, ENTITY_TEMPLATE_ID, ENABLED)
        VALUES
            (@SetNode, @LifecycleWorkflow, @Version, CAST(CAST(@OrderNo AS int) + 1 AS varchar(10)), @ActionNode, 'Set Status to ' + @TargetStatus, 'Set Status to ' + @TargetStatus, 'SET_ENTITY_PROPERTY',
             '<item><key><string>ENTITY_NAME</string></key><value><string>NPH_INSPECTION_REQUEST</string></value><key><string>PROPERTY</string></key><value><string>Status</string></value><key><string>VALUE</string></key><value><string>' + @TargetStatus + '</string></value></item>', '', '', 'T');
    END
    ELSE
    BEGIN
        UPDATE WORKFLOW_NODE
        SET WORKFLOW_ID = @LifecycleWorkflow,
            WORKFLOW_VERSION = @Version,
            ORDER_NUMBER = CAST(CAST(@OrderNo AS int) + 1 AS varchar(10)),
            PARENT_NODE = @ActionNode,
            NAME = 'Set Status to ' + @TargetStatus,
            DESCRIPTION = 'Set Status to ' + @TargetStatus,
            NODE_TYPE = 'SET_ENTITY_PROPERTY',
            PARAMETERS_EXT = '<item><key><string>ENTITY_NAME</string></key><value><string>NPH_INSPECTION_REQUEST</string></value><key><string>PROPERTY</string></key><value><string>Status</string></value><key><string>VALUE</string></key><value><string>' + @TargetStatus + '</string></value></item>',
            DEFAULT_WORKFLOW_ID = '',
            ENTITY_TEMPLATE_ID = '',
            ENABLED = 'T'
        WHERE WORKFLOW_NODE_GUID = @SetNode;
    END

    FETCH NEXT FROM action_cursor INTO @ActionId, @ActionName, @StateId, @TargetStatus, @ActionNode, @SetNode, @OrderNo;
END
CLOSE action_cursor;
DEALLOCATE action_cursor;

DELETE FROM NPH_IR_PRODUCT WHERE LTRIM(RTRIM(REQUEST_ID)) IN ('', '0');
DELETE FROM NPH_IR_LP_TEST_FIELD WHERE LTRIM(RTRIM(REQUEST_ID)) IN ('', '0');
DELETE FROM NPH_IR_LP_TEST WHERE LTRIM(RTRIM(REQUEST_ID)) IN ('', '0');
DELETE FROM NPH_IR_LP_FIELD WHERE LTRIM(RTRIM(REQUEST_ID)) IN ('', '0');
DELETE FROM NPH_IR_LP_ENTRY WHERE LTRIM(RTRIM(REQUEST_ID)) IN ('', '0');
DELETE FROM WORKFLOW_LINK WHERE TABLE_NAME = @Entity AND LTRIM(RTRIM(RECORD_KEY0)) IN ('', '0');
DELETE FROM NPH_INSPECTION_REQUEST WHERE LTRIM(RTRIM(REQUEST_ID)) IN ('', '0');

UPDATE NPH_INSPECTION_REQUEST
SET WORKFLOW_NODE = @LoginCreateNode,
    ENTITY_TEMPLATE_ID = CASE WHEN ENTITY_TEMPLATE_ID = '' THEN @Template ELSE ENTITY_TEMPLATE_ID END,
    ENTITY_TEMPLATE_VERSION = CASE WHEN ENTITY_TEMPLATE_VERSION = '' THEN @Version ELSE ENTITY_TEMPLATE_VERSION END,
    LIFECYCLE_WORKFLOW_ID = CASE WHEN LIFECYCLE_WORKFLOW_ID = '' THEN @LifecycleWorkflow ELSE LIFECYCLE_WORKFLOW_ID END,
    LIFECYCLE_WORKFLOW_VERSION = CASE WHEN LIFECYCLE_WORKFLOW_VERSION = '' THEN @Version ELSE LIFECYCLE_WORKFLOW_VERSION END,
    LIFECYCLE_NODE_ID = CASE WHEN LIFECYCLE_NODE_ID = '' THEN '19940000-0000-0000-0000-000000000101' ELSE LIFECYCLE_NODE_ID END,
    STATUS = CASE WHEN STATUS = '' THEN 'DRAFT' ELSE STATUS END;

MERGE WORKFLOW_LINK AS target
USING (
    SELECT REQUEST_ID FROM NPH_INSPECTION_REQUEST WHERE REMOVEFLAG = 'F'
) AS source
ON target.TABLE_NAME = @Entity AND target.RECORD_KEY0 = source.REQUEST_ID
WHEN MATCHED THEN UPDATE SET WORKFLOW_NODE = @LoginCreateNode
WHEN NOT MATCHED THEN INSERT (TABLE_NAME, RECORD_KEY0, WORKFLOW_NODE)
VALUES (@Entity, source.REQUEST_ID, @LoginCreateNode);

UPDATE MASTER_MENU
SET DESCRIPTION = 'Login Inspection Request',
    SHORT_TEXT = 'Login',
    LIBRARY = '',
    ROUTINE = '',
    DATA_TYPE = 'ACTIVE',
    ACTION_TYPE = 'ADD',
    TASK_NAME = 'NewPharmaInspectionRequestTask',
    TASK_PARAMETERS = @Entity,
    WINDOW_STYLE = 'DEFAULT',
    ICON = 'TEXT_TREE_PLAY'
WHERE PROCEDURE_NUM = 199402;

UPDATE WORKFLOW
SET INCLUDE_IN_MENU = 'F',
    MENU_ICON_ID = '',
    MENU_TEXT = '',
    SUB_MENU_GROUP = '',
    ACTIVE = 'T',
    REMOVEFLAG = 'F'
WHERE WORKFLOW_GUID = @LoginWorkflow
  AND WORKFLOW_VERSION = @Version;

MERGE MASTER_MENU AS target
USING (VALUES
    (199420, 'Add Inspection Request Create Workflow', 'Add', 'ANWF', 'ADD', 'FORM_GREEN', 'Workflow,' + @WorkflowType + ',' + @Entity),
    (199421, 'Copy Inspection Request Create Workflow', 'Copy', 'CNWF', 'COPY', 'INT_COPY', 'Workflow,' + @WorkflowType),
    (199422, 'Modify Inspection Request Create Workflow', 'Modify', 'MNWF', 'MODIFY', 'INT_MODIFY', 'Workflow,' + @WorkflowType),
    (199423, 'Display Inspection Request Create Workflow', 'Display', 'DNWF', 'DISPLAY', 'INT_DISPLAY', 'Workflow,' + @WorkflowType),
    (199424, 'Remove Inspection Request Create Workflow', 'Remove', 'RNWF', 'REMOVE', 'INT_REMOVE', 'Workflow,' + @WorkflowType),
    (199425, 'Restore Inspection Request Create Workflow', 'Restore', 'UNWF', 'RESTORE', 'INT_RESTORE', 'Workflow,' + @WorkflowType),
    (199426, 'New Version of Inspection Request Create Workflow', 'New Version', 'VNWF', 'NEWVERSION', '', 'Workflow,' + @WorkflowType)
) AS source(PROCEDURE_NUM, DESCRIPTION, SHORT_TEXT, MNEMONIC, ACTION_TYPE, ICON, TASK_PARAMETERS)
ON target.PROCEDURE_NUM = source.PROCEDURE_NUM
WHEN MATCHED THEN UPDATE SET
    DESCRIPTION = source.DESCRIPTION,
    SHORT_TEXT = source.SHORT_TEXT,
    MNEMONIC = source.MNEMONIC,
    LIBRARY = '',
    ROUTINE = '',
    TABLE_NAME = 'WORKFLOW',
    DATA_TYPE = 'STATIC',
    ACTION_TYPE = source.ACTION_TYPE,
    TYPE = 'MENU',
    COMMITTED = CASE WHEN source.ACTION_TYPE = 'DISPLAY' THEN 'T' ELSE 'F' END,
    AUX_REPORT = '',
    MODIFIABLE = 'T',
    REMOVEFLAG = 'F',
    ICON = source.ICON,
    ESIG_LEVEL = '',
    ESIG_REASON = '',
    PARAMETERS = '',
    PARAMETERS_ACTIONS = 'BEFORE',
    LIMSML_ENTITY = '',
    LIMSML_ACTION = '',
    CATEGORY = 'Workflow - Inspection Request Create',
    IMPLEMENTATION_TYPE = 'DESIGNER',
    TASK_NAME = 'WorkflowTask',
    TASK_PARAMETERS = source.TASK_PARAMETERS,
    WINDOW_STYLE = 'DEFAULT',
    HAS_ATTACHMENTS = 'F',
    MENU_GROUP_NAME = '',
    WEB_FUNCTION_NAME = '',
    WEB_ENABLED = 'T',
    WEB_AVAILABLE = 'T',
    WEB_ONLY = 'F',
    INTENTIONAL_DUPLICATE = 'F',
    BARCODE_LAYOUT = '',
    DEPRECATED = 'F'
WHEN NOT MATCHED THEN INSERT
    (PROCEDURE_NUM, DESCRIPTION, SHORT_TEXT, MNEMONIC, LIBRARY, ROUTINE, TABLE_NAME, DATA_TYPE, ACTION_TYPE, TYPE,
     COMMITTED, AUX_REPORT, MODIFIABLE, REMOVEFLAG, ICON, ESIG_LEVEL, ESIG_REASON, PARAMETERS, PARAMETERS_ACTIONS,
     LIMSML_ENTITY, LIMSML_ACTION, CATEGORY, IMPLEMENTATION_TYPE, TASK_NAME, TASK_PARAMETERS, WINDOW_STYLE,
     HAS_ATTACHMENTS, MENU_GROUP_NAME, WEB_FUNCTION_NAME, WEB_ENABLED, WEB_AVAILABLE, WEB_ONLY, INTENTIONAL_DUPLICATE,
     BARCODE_LAYOUT, DEPRECATED)
VALUES
    (source.PROCEDURE_NUM, source.DESCRIPTION, source.SHORT_TEXT, source.MNEMONIC, '', '', 'WORKFLOW', 'STATIC',
     source.ACTION_TYPE, 'MENU', CASE WHEN source.ACTION_TYPE = 'DISPLAY' THEN 'T' ELSE 'F' END, '', 'T', 'F',
     source.ICON, '', '', '', 'BEFORE', '', '', 'Workflow - Inspection Request Create', 'DESIGNER', 'WorkflowTask',
     source.TASK_PARAMETERS, 'DEFAULT', 'F', '', '', 'T', 'T', 'F', 'F', '', 'F');

MERGE ROLE_ENTRY AS target
USING (VALUES
    (199420, 'STATIC_MAINTAINER'),
    (199421, 'STATIC_MAINTAINER'),
    (199422, 'STATIC_MAINTAINER'),
    (199423, 'STATIC_DISPLAYER'),
    (199424, 'STATIC_MAINTAINER'),
    (199425, 'STATIC_MAINTAINER'),
    (199426, 'STATIC_MAINTAINER')
) AS source(PROCEDURE_NUM, ROLE_ID)
ON target.PROCEDURE_NUM = source.PROCEDURE_NUM
   AND target.ROLE_ID = source.ROLE_ID
WHEN NOT MATCHED THEN INSERT (PROCEDURE_NUM, ROLE_ID)
VALUES (source.PROCEDURE_NUM, source.ROLE_ID);

DELETE FROM EXPLORER_RMB
WHERE CABINET = 'TABLE_DETAILS'
  AND LTRIM(RTRIM(FOLDER_NUMBER)) = '111'
  AND TRY_CONVERT(int, RMB_NUMBER) BETWEEN 180 AND 188;

INSERT INTO EXPLORER_RMB
    (CABINET, FOLDER_NUMBER, RMB_NUMBER, TYPE, NAME, DESCRIPTION, GROUP_ID, ORDER_NUMBER, PARENT_NUMBER, CONTEXT_ROUTINE, CONTEXT_LIBRARY, CONTEXT_FIELD, CONTEXT_OPERATOR, CONTEXT_VALUE, ALLOW_MULTIPLE, ON_TREE, REFRESH, MENUPROC, [USING], LOCK_REQUIRED, ON_NAVBAR, ON_GRID, BEGIN_GROUP, ONLY_COMMITTED)
VALUES
('TABLE_DETAILS','       111','       180','ITEM','Inspection Request Create Workflow','Add Inspection Request Create Workflow','','       180','        15','','','','','','F','T','T',199420,'','F','T','T','F','F'),
('TABLE_DETAILS','       111','       181','GROUP','Inspection Request Create Workflow','','','       181','         0','','','','','','F','F','F',0,'','F','T','T','F','F'),
('TABLE_DETAILS','       111','       182','ITEM','Copy','Copy Inspection Request Create Workflow','','       182','       181','','','WORKFLOW_TYPE','1',@WorkflowType,'F','F','T',199421,'WORKFLOW_GUID,WORKFLOW_VERSION','F','T','T','F','F'),
('TABLE_DETAILS','       111','       183','ITEM','New Version','New Version of Inspection Request Create Workflow','','       183','       181','','','WORKFLOW_TYPE','1',@WorkflowType,'F','F','T',199426,'WORKFLOW_GUID,WORKFLOW_VERSION','F','T','T','F','F'),
('TABLE_DETAILS','       111','       184','ITEM','Modify','Modify Inspection Request Create Workflow','','       184','       181','','','WORKFLOW_TYPE','1',@WorkflowType,'T','F','T',199422,'WORKFLOW_GUID,WORKFLOW_VERSION','F','T','T','F','F'),
('TABLE_DETAILS','       111','       185','ITEM','Display','Display Inspection Request Create Workflow','','       185','       181','','','WORKFLOW_TYPE','1',@WorkflowType,'T','F','F',199423,'WORKFLOW_GUID,WORKFLOW_VERSION','F','T','T','F','F'),
('TABLE_DETAILS','       111','       186','SEPARATOR','<Separator>','','','       186','       181','','','','','','F','F','F',0,'','F','T','T','F','F'),
('TABLE_DETAILS','       111','       187','ITEM','Remove','Remove Inspection Request Create Workflow','','       187','       181','','','WORKFLOW_TYPE','1',@WorkflowType,'T','F','T',199424,'WORKFLOW_GUID,WORKFLOW_VERSION','F','T','T','F','F'),
('TABLE_DETAILS','       111','       188','ITEM','Restore','Restore Inspection Request Create Workflow','','       188','       181','','','WORKFLOW_TYPE','1',@WorkflowType,'T','F','T',199425,'WORKFLOW_GUID,WORKFLOW_VERSION','F','T','T','F','F');

UPDATE MASTER_MENU
SET ACTION_TYPE = 'STATUS',
    DATA_TYPE = 'ACTIVE',
    TASK_NAME = CASE PROCEDURE_NUM
        WHEN 199410 THEN 'NewPharmaInspectionRequestSubmitTask'
        WHEN 199411 THEN 'NewPharmaInspectionRequestReviewTask'
        WHEN 199412 THEN 'NewPharmaInspectionRequestApproveTask'
        WHEN 199413 THEN 'NewPharmaInspectionRequestRejectTask'
        ELSE TASK_NAME
    END,
    TASK_PARAMETERS = CASE PROCEDURE_NUM
        WHEN 199410 THEN @Entity
        WHEN 199411 THEN @Entity
        WHEN 199412 THEN @Entity
        WHEN 199413 THEN @Entity
        ELSE TASK_PARAMETERS
    END,
    WINDOW_STYLE = 'DEFAULT',
    ICON = CASE PROCEDURE_NUM
        WHEN 199410 THEN 'INT_SUBMIT'
        WHEN 199411 THEN 'INT_VIEW'
        WHEN 199412 THEN 'INT_APPROVE'
        WHEN 199413 THEN 'INT_REMOVE'
        ELSE ICON
    END
WHERE PROCEDURE_NUM IN (199410, 199411, 199412, 199413);

DELETE FROM EXPLORER_RMB
WHERE CABINET IN ('LABTABLE', 'TABLE_DETAILS')
  AND LTRIM(RTRIM(FOLDER_NUMBER)) = '1994';

INSERT INTO EXPLORER_RMB
    (CABINET, FOLDER_NUMBER, RMB_NUMBER, TYPE, NAME, DESCRIPTION, GROUP_ID, ORDER_NUMBER, PARENT_NUMBER, CONTEXT_ROUTINE, CONTEXT_LIBRARY, CONTEXT_FIELD, CONTEXT_OPERATOR, CONTEXT_VALUE, ALLOW_MULTIPLE, ON_TREE, REFRESH, MENUPROC, [USING], LOCK_REQUIRED, ON_NAVBAR, ON_GRID, BEGIN_GROUP, ONLY_COMMITTED)
VALUES
('TABLE_DETAILS','      1994','1','GROUP','Inspection Request','Inspection Request actions','','1','0','','','','','','F','F','F',0,'','F','F','T','F','F'),
('TABLE_DETAILS','      1994','2','ITEM','Login...','Create a new inspection request through workflow login','','2','0','','','','','','F','F','T',199402,'','F','T','T','F','F'),
('TABLE_DETAILS','      1994','3','ITEM','Modify...','Modify this inspection request','','3','1','','','','','','T','F','T',199403,'REQUEST_ID','F','T','T','F','F'),
('TABLE_DETAILS','      1994','4','ITEM','Display...','Display this inspection request','','4','1','','','','','','T','F','F',199404,'REQUEST_ID','F','T','T','F','F'),
('TABLE_DETAILS','      1994','5','SEPARATOR','<Separator>','','','5','1','','','','','','F','F','F',0,'','F','T','T','F','F'),
('TABLE_DETAILS','      1994','6','ITEM','Remove','Remove this inspection request','','6','1','','','REMOVEFLAG','1','No','T','F','T',199405,'REQUEST_ID','F','T','T','F','F'),
('TABLE_DETAILS','      1994','7','ITEM','Restore','Restore this inspection request','','7','1','','','REMOVEFLAG','1','Yes','T','F','T',199406,'REQUEST_ID','F','T','T','F','F'),
('TABLE_DETAILS','      1994','8','SEPARATOR','<Separator>','','','8','1','','','','','','F','F','F',0,'','F','T','T','T','F'),
('TABLE_DETAILS','      1994','9','GROUP','Set Data Assignment Value','Set Data Assignment Value','','9','1','','','','','','F','F','F',0,'','F','T','T','F','F'),
('TABLE_DETAILS','      1994','10','ITEM','ProjectId...','Set ProjectId data assignment value','','10','9','','','','','','F','F','T',199407,'REQUEST_ID','F','T','T','F','T'),
('TABLE_DETAILS','      1994','11','ITEM','ProductLink...','Set ProductLink data assignment value','','11','9','','','','','','F','F','T',199408,'REQUEST_ID','F','T','T','F','T'),
('TABLE_DETAILS','      1994','12','ITEM','SamplingPoint...','Set SamplingPoint data assignment value','','12','9','','','','','','F','F','T',199409,'REQUEST_ID','F','T','T','F','T'),
('TABLE_DETAILS','      1994','13','SEPARATOR','<Separator>','','','13','1','','','','','','F','F','F',0,'','F','T','T','T','F'),
('TABLE_DETAILS','      1994','18','ITEM','Execute','Execute this approved inspection request','','18','1','','','','','','F','F','T',199414,'REQUEST_ID','F','T','T','F','F');

INSERT INTO EXPLORER_RMB
    (CABINET, FOLDER_NUMBER, RMB_NUMBER, TYPE, NAME, DESCRIPTION, GROUP_ID, ORDER_NUMBER, PARENT_NUMBER, CONTEXT_ROUTINE, CONTEXT_LIBRARY, CONTEXT_FIELD, CONTEXT_OPERATOR, CONTEXT_VALUE, ALLOW_MULTIPLE, ON_TREE, REFRESH, MENUPROC, [USING], LOCK_REQUIRED, ON_NAVBAR, ON_GRID, BEGIN_GROUP, ONLY_COMMITTED)
SELECT
    'LABTABLE', FOLDER_NUMBER, RMB_NUMBER, TYPE, NAME, DESCRIPTION, GROUP_ID, ORDER_NUMBER, PARENT_NUMBER, CONTEXT_ROUTINE, CONTEXT_LIBRARY, CONTEXT_FIELD, CONTEXT_OPERATOR, CONTEXT_VALUE, ALLOW_MULTIPLE, ON_TREE, REFRESH, MENUPROC, [USING], LOCK_REQUIRED, ON_NAVBAR, ON_GRID, BEGIN_GROUP, ONLY_COMMITTED
FROM EXPLORER_RMB
WHERE CABINET = 'TABLE_DETAILS'
  AND LTRIM(RTRIM(FOLDER_NUMBER)) = '1994';

DELETE FROM FORM_PAGE
WHERE FORM = 'NPH_INSPECTION_REQUEST'
  AND PAGE = 'InspectionPage';

SELECT 'NPH Inspection Request workflow login migration applied.' AS RESULT;
