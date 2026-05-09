using System;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Server;

namespace NewPharma.InspectionRequest
{
    internal sealed class InspectionRequestSnapshotService
    {
        private readonly IEntityManager _entityManager;

        public InspectionRequestSnapshotService(IEntityManager entityManager)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        }

        public void EnsureSnapshot(IEntity request)
        {
            if (request == null)
            {
                return;
            }

            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            string loginPlanId = GetString(request, InspectionRequestConstants.FieldLoginPlanId);
            string loginPlanVersion = GetString(request, InspectionRequestConstants.FieldLoginPlanVersion);

            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(loginPlanId))
            {
                return;
            }

            if (HasSnapshotRows(requestId))
            {
                return;
            }

            CopyDataAssignment(requestId, loginPlanId, loginPlanVersion);
            CopyProductSpec(requestId, loginPlanId, loginPlanVersion);
            _entityManager.Commit();
        }

        private bool HasSnapshotRows(string requestId)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableIrLoginPlanEntry);
            query.AddEquals("REQUEST_ID", requestId);
            return _entityManager.Select(query).Count > 0;
        }

        private void CopyDataAssignment(string requestId, string loginPlanId, string loginPlanVersion)
        {
            IQuery entryQuery = _entityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanEntry);
            entryQuery.AddEquals("IDENTITY", loginPlanId);
            if (!string.IsNullOrWhiteSpace(loginPlanVersion))
            {
                entryQuery.AddEquals("VERSION", loginPlanVersion);
            }

            foreach (IEntity sourceEntry in _entityManager.Select(entryQuery))
            {
                decimal orderNumber = GetDecimal(sourceEntry, "ORDER_NUMBER");
                IEntity targetEntry = _entityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanEntry);
                targetEntry.Set("REQUEST_ID", requestId);
                targetEntry.Set("ORDER_NUMBER", orderNumber);
                targetEntry.Set("PARENT_ORDER_NUMBER", GetDecimal(sourceEntry, "PARENT_ENTRY_ORDER_NUMBER"));
                targetEntry.Set("SOURCE_LOGIN_PLAN_ID", loginPlanId);
                targetEntry.Set("SOURCE_LOGIN_PLAN_VERSION", GetValue(sourceEntry, "VERSION"));
                targetEntry.Set("SOURCE_ORDER_NUMBER", orderNumber);
                targetEntry.Set("TABLE_NAME", GetValue(sourceEntry, "TABLE_NAME"));
                targetEntry.Set("NODE_NAME", GetValue(sourceEntry, "NODE_NAME"));
                targetEntry.Set("LOGIN_WORKFLOW_ID", GetValue(sourceEntry, "LOGIN_WORKFLOW_ID"));
                targetEntry.Set("LOGIN_WORKFLOW_VERSION", GetValue(sourceEntry, "LOGIN_WORKFLOW_VERSION"));
                targetEntry.Set("USE_LAST_ACTIVE_LOGIN_WORKFLOW", GetValue(sourceEntry, "USE_LAST_ACTIVE_LOGIN_WORKFLOW"));
                targetEntry.Set("ENTITY_TEMPLATE_ID", GetValue(sourceEntry, "ENTITY_TEMPLATE_ID"));
                targetEntry.Set("ENTITY_TEMPLATE_VERSION", GetValue(sourceEntry, "ENTITY_TEMPLATE_VERSION"));
                targetEntry.Set("LOGIN_CONDITION", GetValue(sourceEntry, "LOGIN_CONDITION"));
                targetEntry.Set("COUNT_EXPRESSION", GetValue(sourceEntry, "COUNT_EXPRESSION"));
                targetEntry.Set("MODIFIABLE", true);
                targetEntry.Set("REMOVEFLAG", false);
                _entityManager.Transaction.Add(targetEntry);

                CopyEntryFields(requestId, loginPlanId, sourceEntry, orderNumber);
                CopyEntryTests(requestId, loginPlanId, sourceEntry, orderNumber);
            }
        }

        private void CopyEntryFields(string requestId, string loginPlanId, IEntity sourceEntry, decimal parentOrderNumber)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanField);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", GetValue(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_ORDER_NUMBER", parentOrderNumber);

            foreach (IEntity sourceField in _entityManager.Select(query))
            {
                IEntity targetField = _entityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanField);
                targetField.Set("REQUEST_ID", requestId);
                targetField.Set("PARENT_ORDER_NUMBER", parentOrderNumber);
                targetField.Set("ORDER_NUMBER", GetDecimal(sourceField, "ORDER_NUMBER"));
                targetField.Set("PROPERTY", GetValue(sourceField, "PROPERTY"));
                targetField.Set("VALUE", GetValue(sourceField, "VALUE"));
                targetField.Set("OVERRIDE_VALUE", GetValue(sourceField, "VALUE"));
                targetField.Set("MODIFIABLE", true);
                _entityManager.Transaction.Add(targetField);
            }
        }

        private void CopyEntryTests(string requestId, string loginPlanId, IEntity sourceEntry, decimal parentOrderNumber)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanTest);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", GetValue(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_ORDER_NUMBER", parentOrderNumber);

            foreach (IEntity sourceTest in _entityManager.Select(query))
            {
                decimal orderNumber = GetDecimal(sourceTest, "ORDER_NUMBER");
                IEntity targetTest = _entityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanTest);
                targetTest.Set("REQUEST_ID", requestId);
                targetTest.Set("PARENT_ORDER_NUMBER", parentOrderNumber);
                targetTest.Set("ORDER_NUMBER", orderNumber);
                targetTest.Set("ANALYSIS_IDENTITY", GetValue(sourceTest, "ANALYSIS_IDENTITY"));
                targetTest.Set("ANALYSIS_VERSION", GetValue(sourceTest, "ANALYSIS_VERSION"));
                targetTest.Set("COMP_LIST_ANALYSIS", GetValue(sourceTest, "COMP_LIST_ANALYSIS"));
                targetTest.Set("COMP_LIST_ANALYSIS_VERSION", GetValue(sourceTest, "COMP_LIST_ANALYSIS_VERSION"));
                targetTest.Set("COMP_LIST_IDENTITY", GetValue(sourceTest, "COMP_LIST_IDENTITY"));
                targetTest.Set("TEST_SCHEDULE_ID", GetValue(sourceTest, "TEST_SCHEDULE_ID"));
                targetTest.Set("RED_TEST_ID", GetValue(sourceTest, "RED_TEST_ID"));
                targetTest.Set("RED_TEST_VERSION", GetValue(sourceTest, "RED_TEST_VERSION"));
                targetTest.Set("LAST_ACTIVE_VERSION", GetValue(sourceTest, "LAST_ACTIVE_VERSION"));
                targetTest.Set("MODIFIABLE", true);
                _entityManager.Transaction.Add(targetTest);

                CopyTestFields(requestId, loginPlanId, sourceEntry, sourceTest, parentOrderNumber, orderNumber);
            }
        }

        private void CopyTestFields(string requestId, string loginPlanId, IEntity sourceEntry, IEntity sourceTest, decimal entryOrderNumber, decimal testOrderNumber)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanTestField);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", GetValue(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_PARENT_ORDER_NUMBER", entryOrderNumber);
            query.AddEquals("PARENT_ORDER_NUMBER", testOrderNumber);

            foreach (IEntity sourceField in _entityManager.Select(query))
            {
                IEntity targetField = _entityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanTestField);
                targetField.Set("REQUEST_ID", requestId);
                targetField.Set("PARENT_PARENT_ORDER_NUMBER", entryOrderNumber);
                targetField.Set("PARENT_ORDER_NUMBER", testOrderNumber);
                targetField.Set("ORDER_NUMBER", GetDecimal(sourceField, "ORDER_NUMBER"));
                targetField.Set("PROPERTY", GetValue(sourceField, "PROPERTY"));
                targetField.Set("VALUE", GetValue(sourceField, "VALUE"));
                targetField.Set("OVERRIDE_VALUE", GetValue(sourceField, "VALUE"));
                targetField.Set("MODIFIABLE", true);
                _entityManager.Transaction.Add(targetField);
            }
        }

        private void CopyProductSpec(string requestId, string loginPlanId, string loginPlanVersion)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableMlpHeader);
            query.AddEquals("LOGIN_PLAN_ID", loginPlanId);
            if (!string.IsNullOrWhiteSpace(loginPlanVersion))
            {
                query.AddEquals("LOGIN_PLAN_VERSION", loginPlanVersion);
            }

            decimal orderNumber = 1;
            foreach (IEntity product in _entityManager.Select(query))
            {
                IEntity targetProduct = _entityManager.CreateEntity(InspectionRequestConstants.TableIrProduct);
                targetProduct.Set("REQUEST_ID", requestId);
                targetProduct.Set("ORDER_NUMBER", orderNumber++);
                targetProduct.Set("PRODUCT_ID", GetValue(product, "IDENTITY"));
                targetProduct.Set("PRODUCT_VERSION", GetValue(product, "PRODUCT_VERSION"));
                targetProduct.Set("PRODUCT_CODE", GetValue(product, "PRODUCT_CODE"));
                targetProduct.Set("PRODUCT_DESCRIPTION", GetValue(product, "DESCRIPTION"));
                targetProduct.Set("TEST_SCHEDULE_ID", GetValue(product, "TEST_SCHEDULE"));
                targetProduct.Set("INSPECTION_PLAN", GetValue(product, "INSPECTION_PLAN"));
                targetProduct.Set("MODIFIABLE", true);
                targetProduct.Set("REMOVEFLAG", false);
                _entityManager.Transaction.Add(targetProduct);
            }
        }

        private static object GetValue(IEntity entity, string fieldName)
        {
            return entity.Get(fieldName);
        }

        private static string GetString(IEntity entity, string fieldName)
        {
            object value = entity.Get(fieldName);
            return value?.ToString() ?? string.Empty;
        }

        private static decimal GetDecimal(IEntity entity, string fieldName)
        {
            object value = entity.Get(fieldName);
            if (value == null)
            {
                return 0;
            }

            return decimal.TryParse(value.ToString(), out decimal parsed) ? parsed : 0;
        }
    }
}
