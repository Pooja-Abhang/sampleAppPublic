using Constants;
using DAL;
using Entities;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BAL
{
    public class BuildModelBAL
    {
        static string _pageName = MethodBase.GetCurrentMethod().DeclaringType.Name;

        /// <summary>
        /// Get build activity by Status
        /// </summary>
        /// <param name="vBuildStatus"></param>
        /// <returns></returns>
        public BuildActivity GetBuildActivity(string vBuildStatus)
        {
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                //The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                                 , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                                 , IntelliConstantMessage.SERVER);

                BuildActivityDAL buildModelDALObj = new BuildActivityDAL();
                List<BuildActivity> lstBuildModelData = new List<BuildActivity>();

                // Get Bond Messages From DB and convert into BondResponse class
                lstBuildModelData = IntelliDataTableToList.ConvertToList<BuildActivity>(buildModelDALObj.GetBuildActivity(vBuildStatus));

                //The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                                 , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                                 , IntelliConstantMessage.SERVER);
                return lstBuildModelData.FirstOrDefault();
            }
            catch// (Exception ex)
            {
                //The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + " - Error Occured"
                                                 , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                                                 , IntelliConstantMessage.SERVER);
                throw;
            }
        }

        /// <summary>
        /// This method update buildmodel activity Status
        /// </summary>
        /// <param name="vBuildActivityId"></param>
        /// <param name="vBuildStatus"></param>
        public void UpdateBuildActivity(BuildActivity vBuildActivityObj)
        {
            // Here we Get Method Name for Log
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                BuildActivityDAL buildModelDALObj = new BuildActivityDAL();
                buildModelDALObj.UpdateBuildActivity(vBuildActivityObj);

                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);
            }
            catch //(Exception ex)
            {
                //The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + "- Error Occured "
                                                 , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                                                 , IntelliConstantMessage.SERVER);
                throw;
            }
        }

        /// <summary>
        /// This Method for Insert Build Activity Details Table Status
        /// </summary>
        /// <returns></returns>
        public void InsertBuildActivityDetail(BuildActivityDetail vbuildActivityDetailObj)
        {
            // Here we Get Method Name for Log 
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                BuildActivityDetailDAL buildModelActivityDetailDALObj = new BuildActivityDetailDAL();
                buildModelActivityDetailDALObj.InsertBuildActivityDetail(vbuildActivityDetailObj);

                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

            }
            catch //(Exception ex)
            {
                //The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + "- Error Occured "
                                                 , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                                                 , IntelliConstantMessage.SERVER);
                throw;
            }
        }

        /// <summary>
        /// This Method For Get Table Data 
        /// </summary>
        /// <returns></returns>
        public StringBuilder GetTableData(BuildActivity buildActivityModelObj)
        {
            // Here we Get Method Name for Log 
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                StringBuilder sbTablesData = new StringBuilder();

                IntentMaintDAL intentMaintDALObj = new IntentMaintDAL();

                // Here we Get Table DataTabless
                DataTable dataTable = intentMaintDALObj.GetTablesData( buildActivityModelObj);

                if (dataTable.Rows.Count > 0)
                {
                    foreach (DataRow row in dataTable.Rows)
                        sbTablesData.AppendLine(row[0].ToString());
                }

                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                return sbTablesData;
            }
            catch //(Exception ex)
            {
                //The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + "- Error Occured "
                                                 , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                                                 , IntelliConstantMessage.SERVER);
                throw;
            }
        }
    }
}
