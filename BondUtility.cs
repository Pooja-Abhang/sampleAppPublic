using Constants;
using DAL;
using Entities;
using System;
using System.Configuration;
using System.Reflection;
using System.Threading;

namespace BAL
{
    public class BondUtility
    {
        static string _pageName = MethodBase.GetCurrentMethod().DeclaringType.Name;
        static string _UtlityUser = ConfigurationManager.AppSettings["UtilityUser"].ToString();

        /// <summary>
        /// This Method for Build Model
        /// </summary>
        /// <param name="vActivityId"></param>
        /// <returns></returns>
        public  static void InsertActivityDetails(BuildActivity vBuildActivityModelObj, string vDescription, string vLogType)
        {
            // Here we Get Method Name for Log
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            BuildModelBAL buildModelBALObj = new BuildModelBAL();

            BondNotificationBAL bondNotificationBALObj = new BondNotificationBAL();

            try
            {
                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                // Here we Insert Log 
                BuildActivityDetail buildActivityDetailObj = new BuildActivityDetail();
                buildActivityDetailObj.BuildActivityId = vBuildActivityModelObj.BuildActivityId;
                buildActivityDetailObj.BuildDescription = vDescription;
                buildActivityDetailObj.BuildLogType = vLogType;
                buildActivityDetailObj.TenantID = vBuildActivityModelObj.TenantID;

                // Here we Insert Activity Log Data
                buildModelBALObj.InsertBuildActivityDetail(buildActivityDetailObj);

                Notifications notificationsObj = new Notifications();
                notificationsObj.NotificationTo = vBuildActivityModelObj.BondCreatedBy;
                notificationsObj.NotificationFrom = _UtlityUser;
                notificationsObj.Subject = "GenerateFiles" + "," + vBuildActivityModelObj.BuildVersion;
                notificationsObj.Module = "GenerateFiles";
                notificationsObj.NotificationDesc = vDescription;
                notificationsObj.BondCreatedBy = _UtlityUser;
                notificationsObj.TenantID = vBuildActivityModelObj.TenantID;

                bondNotificationBALObj.InsertNotificationsStatus(notificationsObj,  vBuildActivityModelObj);

                //Thread.Sleep(120000);

                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);
            }
            catch //(Exception ex)
            {
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + ": Error Occured"
                                , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                                , IntelliConstantMessage.SERVER);

                throw;
            }
        }
    }
}
