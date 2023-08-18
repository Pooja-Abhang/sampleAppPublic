using Constants;
using DAL;
using Entities;
using GenerateFiles;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BAL
{
    public class CreateFilesBAL
    {
        static string _pageName = MethodBase.GetCurrentMethod().DeclaringType.Name;
        string _intentCnt = ConfigurationManager.AppSettings["IntentCnt"].ToString();
        static string _lemmaEndPoint = ConfigurationManager.AppSettings["LemmaEndPoint"].ToString();
        static string _lemmaAuth = ConfigurationManager.AppSettings["LemmaAuth"].ToString();

        LookUpOps _lookUpOpsObj = new LookUpOps();

        // Here we create HashTables for Stories, lookups and intents
        Hashtable _htStories = new Hashtable();
        Hashtable _htLookups = new Hashtable();
        Hashtable _htIntents = new Hashtable();

        /// <summary>
        /// Generate and export NLu file
        /// </summary>
        /// <returns></returns>
        public void GenerateFiles(BuildActivity vBuildActivityModelObj)
        {
            // Here we Get Method Name for Log
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            // Here we define string Builder
            StringBuilder dbNLUData = new StringBuilder();
            DataTable dtLookup = new DataTable();

            // Create Class Object
            BuildModelBAL buildModelBALObj = new BuildModelBAL();

            try
            {
                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                // Step 2: Create Nlu File 
                Console.WriteLine("####################### Create Files - START ##########################");

                // Here we Create an Object of raw Intent Class
                IntentMaintDAL intentMntDALObj = new IntentMaintDAL();

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Get RawIntent Data From DB - START", IntelliConstantMessage.INFO);

                Console.WriteLine("*************** Get RawIntent Data - START ****************");

                // Here we Get Raw Intent Data From Database
                List<RawIntentData> lstRawIntentDataObj = intentMntDALObj.GetAllRawIntents(vBuildActivityModelObj);

                if (Convert.ToInt32(_intentCnt) != 0)
                    lstRawIntentDataObj = lstRawIntentDataObj.Take(Convert.ToInt32(_intentCnt)).ToList();

                Console.WriteLine("*************** Get RawIntent Data - END ****************");

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Get RawIntent From DB - END", IntelliConstantMessage.INFO);

                //lstRawIntentDataObj = lstRawIntentDataObj.Where(x => x.ModelName == _moldelName).ToList();

                // This lines of Code execute only when ApplyLookupsToAll is true
                if (vBuildActivityModelObj.ApplyLookupstoAll == true)
                {
                    // This line of code for get LookUp data
                    LookupData vlookupDataObj = new LookupData();
                    vlookupDataObj.Language = vBuildActivityModelObj.BuildLang;

                    //get lookup data
                    LookupDataDAL lookupDataDALObj = new LookupDataDAL();

                    // here we Insert Activity Log
                    BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Get LookUp data From DB - START", IntelliConstantMessage.INFO);

                    Console.WriteLine("*************** Get LookUp Data - START ****************");

                    // Here we get lookup data from tables
                    dtLookup = lookupDataDALObj.GetAllLookupData(vlookupDataObj, vBuildActivityModelObj);

                    Console.WriteLine("*************** Get lookUp Data - END ****************");

                    // here we Insert Activity Log
                    BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Get LookUp data From DB - END", IntelliConstantMessage.INFO);

                    // here we Insert Activity Log
                    BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Generate LookUp Files - START", IntelliConstantMessage.INFO);

                    Console.WriteLine("*************** Generate LookUp Files - START ****************");

                    // Here we Call Generate LookUps Table
                    GenerateLookupFiles(dtLookup, vBuildActivityModelObj.GenerateModelPath);

                    Console.WriteLine("*************** Generate LookUp Files - END ****************");

                    // here we Insert Activity Log
                    BondUtility.InsertActivityDetails(vBuildActivityModelObj, "LookUp Files Generated Succesfully - START", IntelliConstantMessage.INFO);

                }

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Create NLU Files - START", IntelliConstantMessage.INFO);

                Console.WriteLine("*************** Create NLU Files - START ****************");

                //delete all NLU files before generating files
                DirectoryInfo baseDir = new DirectoryInfo(vBuildActivityModelObj.GenerateModelPath + "\\nlu\\");

                FileInfo[] files = baseDir.GetFiles();

                // This Loop for delete files
                foreach (FileInfo file in files)
                {
                    file.Delete();
                }

                //delete all CORE files before generating files
                DirectoryInfo baseDirCore = new DirectoryInfo(vBuildActivityModelObj.GenerateModelPath + "\\core\\");

                FileInfo[] filesCore = baseDirCore.GetFiles();

                // This Loop for delete files
                foreach (FileInfo file in filesCore)
                {
                    file.Delete();
                }


                //Step 2: Put all intents and their uttarences together and Create Seprate NLU Files
                foreach (RawIntentData rawIntentObj in lstRawIntentDataObj)
                {

                    //added by pooja for MU 
                    // utterances are stored in different table now
                    DataTable Muttr = intentMntDALObj.GetIntentUttarnaces(rawIntentObj.IntentName, vBuildActivityModelObj);

                    string MUstr = string.Empty;
                    if (Muttr.Rows.Count != 0)
                    {
                        foreach (DataRow dt in Muttr.Rows)
                        {

                            if (MUstr == "")
                                MUstr = "|- " + dt["Utterance"].ToString();
                            else
                                MUstr = MUstr + "|- " + dt["Utterance"].ToString();
                        }
                    }

                    if (MUstr != "")
                    {
                        rawIntentObj.MultipleUtterance = MUstr.Remove(0, 1);
                    }

                    Console.WriteLine("Applying lookup Start for - " + rawIntentObj.IntentName);

                    // here we Insert Activity Log

                    new Task(() =>
                    {
                        BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Create NLU for " + rawIntentObj.IntentName + " Intent - START", IntelliConstantMessage.INFO);
                    }).Start();


                    StringBuilder sbNLUIntentData = new StringBuilder();

                    if (rawIntentObj.MultipleUtterance != "")
                    {
                        // Here we get intentname
                        string strIntentName = rawIntentObj.IntentName;
                        strIntentName = "Intent" + strIntentName;

                        // Here we add Intentname in String Builder
                        dbNLUData.AppendLine("## intent:" + strIntentName);
                        sbNLUIntentData.AppendLine("## intent:" + strIntentName);

                        // Here we Create Multi Utterances Array by spliting '\n'
                        //List<string> multiUtt = rawIntentObj.MultipleUtterance.Split('\n').ToList();
                        List<string> multiUtt = rawIntentObj.MultipleUtterance.Split('|').ToList();
                        string str = string.Empty;

                        List<string> removeLookup = new List<string>();

                        // Here we Remove Lookups from Utternces
                        foreach (var item in multiUtt)
                        {
                            string Sentance = _lookUpOpsObj.RemoveLkp(item.Trim());

                            Sentance = Sentance.Replace(" .", "").Replace(".", "").Replace("?", "");

                            //Here we trim starting space of sentense
                            Sentance = Sentance.Trim();

                            // Here we Add Removed Lookups sentance in Remove lookup list
                            removeLookup.Add(Sentance);
                        }

                        // Here we Create Multi Utterances Array by spliting '\n'
                        List<string> LemmatizeUtter = GetLemmaSentace(removeLookup);

                        // Here Add All Utterance in NLU Intent 
                        // foreach (string strSentence in multiUtt)
                        for (int i = 0; i < LemmatizeUtter.Count; i++)
                        {
                            string strSentence = LemmatizeUtter[i];

                            if (strSentence != "")
                            {
                                string strApplyLookup = string.Empty;

                                // Here we check Apply Lookups True or not
                                if (vBuildActivityModelObj.ApplyLookupstoAll == true)
                                {
                                    LookUpOps lookUpOpsObj = new LookUpOps();

                                    try
                                    {
                                        //str = _lookUpOpsObj.RemoveLkp(strSentence);

                                        //// here we replace \n \r with blanck from sentense.
                                        //str = str.Replace("\n", "").Replace("\r", "");

                                        // here we call Lemma API and Get lemmatize sentance
                                        //List<string> sampleLemmaString = GetLemmaSentace(str);
                                        if(strSentence !=" ")
                                        strApplyLookup = lookUpOpsObj.applyLKPForOneSentence(strSentence.Trim(), dtLookup, strIntentName, LemmatizeUtter[i].Trim());
                                    }
                                    catch (Exception ex)
                                    {
                                        strApplyLookup = strSentence + " # Error";
                                        IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + ": Error Occured" + ex.Message + Environment.NewLine + ex.StackTrace
                                        , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                                        , IntelliConstantMessage.SERVER);
                                    }
                                }
                                else
                                    strApplyLookup = strSentence;

                                if (!strSentence.Contains("## "))
                                {
                                    dbNLUData.AppendLine(strApplyLookup.Trim());
                                    sbNLUIntentData.AppendLine(strApplyLookup.Trim());
                                }
                            }
                        }

                        if (Directory.Exists(vBuildActivityModelObj.GenerateModelPath + "\\nlu"))
                        {
                            // Here we Create Separate NLU Files
                            File.WriteAllText(vBuildActivityModelObj.GenerateModelPath + "\\nlu\\" + strIntentName + ".md", sbNLUIntentData.ToString());
                        }
                        else
                        {
                            Directory.CreateDirectory(vBuildActivityModelObj.GenerateModelPath + "\\nlu");

                            // Here we Create Separate NLU Files
                            File.WriteAllText(vBuildActivityModelObj.GenerateModelPath + "\\nlu\\" + strIntentName + ".md", sbNLUIntentData.ToString());
                        }
                    }

                    new Task(() =>
                    {
                        // here we Insert Activity Log
                        BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Create NLU for " + rawIntentObj.IntentName + " Intent - END", IntelliConstantMessage.INFO);
                    }).Start();

                    Console.WriteLine("Applying lookup End for - " + rawIntentObj.IntentName);
                }

                Console.WriteLine("*************** Create NLU Files - END ****************");

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Create NLU Files - END", IntelliConstantMessage.INFO);

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Create NLU Files LookUp - START", IntelliConstantMessage.INFO);

                Console.WriteLine("*************** Create NLU Lookup Files - START ****************");

                // Here Get Nlu into object
                List<sbIntent> lstsbIntent = GetNLUDataInObjects(dbNLUData.ToString());

                // Here we Create HashTables for Lookups and Stories
                PopulateHashTables(lstsbIntent);

                // Here we order htlookups
                var orderedKeys = _htLookups.Keys.Cast<string>().OrderBy(c => c);

                StringBuilder sbNLULookUpData = new StringBuilder();

                // Here we Create nlu File LookUps
                foreach (String k in orderedKeys)
                {
                    if (k != "")
                    {
                        switch (k)
                        {
                            // Here we Add Regex for Age Lkp
                            case "Lkp_Age":
                                sbNLULookUpData.AppendLine("## regex:" + k);
                                sbNLULookUpData.AppendLine("  - [0-9]{2}");
                                break;

                            // Here we Add Regex for Amount Lkp
                            case "Lkp_Amount":
                                sbNLULookUpData.AppendLine("## regex:" + k);
                                sbNLULookUpData.AppendLine("  - [0-9]{4}");
                                break;

                            default:
                                if (File.Exists(vBuildActivityModelObj.GenerateModelPath + "/lookupData/" + k + ".txt"))
                                {
                                    sbNLULookUpData.AppendLine("## lookup:" + k);
                                    sbNLULookUpData.AppendLine("    data/lookupData/" + k + ".txt");
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                        }
                    }
                }

                // Here we Create nlu LookUp Files
                File.WriteAllText(vBuildActivityModelObj.GenerateModelPath + "\\nlu\\" + "lookup.md", sbNLULookUpData.ToString());

                Console.WriteLine("*************** Create NLU Lookup Files - END ****************");

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Create NLU Files LookUp - END", IntelliConstantMessage.INFO);

                Console.WriteLine("*************** Create NLU Files - END ****************");

                // Here we Create Stories
                GetStoriesFileData(vBuildActivityModelObj);

                // Here we Generate domain.yml File
                GetDomainFileData(vBuildActivityModelObj);

                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);


            }
            catch (Exception ex)
            {
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + ": Error Occured"
                , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                , IntelliConstantMessage.SERVER);

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Error In Creating NLU Files: " + ex.Message + "Inner exception:" + ex.InnerException.Message, IntelliConstantMessage.ERROR);

                throw;
            }
        }

        /// <summary>
        /// This Method for Create Stories File
        /// </summary>
        /// <param name="vFolderPath"></param>
        /// <param name="vActivityId"></param>
        /// <returns></returns>
        public void GetStoriesFileData(BuildActivity vBuildActivityModelObj)
        {
            // Here we Get Method Name for Log
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            BuildModelBAL buildModelBALObj = new BuildModelBAL();

            try
            {
                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                Console.WriteLine("*************** Create Stories Files - START *************");

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Create Stories - START", IntelliConstantMessage.INFO);

                // Here we Writting Stories in text file
                if (_htStories.Count > 0)
                {
                    Console.WriteLine("************* Stories writing START *************** \n");

                    //append fallback story
                    string storyName = "## FallbackIntent story 001\n";
                    string storyContent = "* FallbackIntent\n  - Action_Intent\n\n";

                    _htStories.Add(storyName, storyContent);

                    // Here we Order htstories ASC
                    var orderedKeys = _htStories.Keys.Cast<string>().OrderBy(c => c);

                    // local variables
                    string strintentName = string.Empty;
                    int count = 0;

                    // String Builder for Hold Stories
                    StringBuilder sb = new StringBuilder();

                    // here we add Story Into String Builder
                    foreach (String k in orderedKeys)
                    {
                        //Here we Get Intent Name From Story Title
                        string strIntentName = k.Replace("## ", "");
                        string intentName = strIntentName.Split(' ')[0];

                        // Increment Count
                        count++;

                        // Here we check weather  string Builder Contains Intentname or not/ Intentname is Fallback
                        if (sb.ToString().Contains(intentName) || intentName.ToUpper() == "FALLBACKINTENT")
                        {
                            // Here we add Stroy Title
                            sb.Append(k);

                            // Here we add stories Actions
                            sb.Append(_htStories[k]);

                            //Here we Get Intent Name From Story Title
                            string IntentName = k.Replace("## ", "");
                            strintentName = strIntentName.Split(' ')[0];

                            if (orderedKeys.Count() == count)
                            {
                                // Here we Create Stories.md file
                                File.WriteAllText(vBuildActivityModelObj.GenerateModelPath + "\\core\\" + strintentName + ".md", sb.ToString());
                            }

                        }
                        else
                        {
                            if (Directory.Exists(vBuildActivityModelObj.GenerateModelPath + "\\core"))
                            {
                                // Here we Create Stories.md file
                                File.WriteAllText(vBuildActivityModelObj.GenerateModelPath + "\\core\\" + strintentName + ".md", sb.ToString());
                            }
                            else
                            {
                                Directory.CreateDirectory(vBuildActivityModelObj.GenerateModelPath + "\\core");

                                // Here we Create Stories.md file
                                File.WriteAllText(vBuildActivityModelObj.GenerateModelPath + "\\core\\" + strintentName + ".md", sb.ToString());
                            }

                            // Here we clear string Builder
                            sb = new StringBuilder();

                            // Here we add Stroy Title
                            sb.Append(k);

                            // Here we add stories Actions
                            sb.Append(_htStories[k]);

                            //Here we Get Intent Name From Story Title
                            string IntentName = k.Replace("## ", "");
                            strintentName = strIntentName.Split(' ')[0];
                        }

                    }
                }

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Create Stories - END", IntelliConstantMessage.INFO);

                Console.WriteLine("*************** Create Stories Files - END *************");

                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

            }
            catch (Exception ex)
            {
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + ": Error Occured"
                                , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                                , IntelliConstantMessage.SERVER);

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Error in Create Stories file : " + ex.Message + "Inner exception:" + ex.InnerException.Message, IntelliConstantMessage.ERROR);

                throw;
            }
        }

        /// <summary>
        /// This Method For Create Domain File
        /// </summary>
        /// <param name="vFolderPath"></param>
        /// <param name="vActivityId"></param>
        public void GetDomainFileData(BuildActivity vBuildActivityModelObj)
        {

            // Here we Get Method Name for Log
            string strMethodName = MethodBase.GetCurrentMethod().Name;
            StringBuilder sbReturnValue = new StringBuilder();

            BuildModelBAL buildModelBALObj = new BuildModelBAL();
            bool AOIntentExist = false;
            try
            {
                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                Console.WriteLine("*************** Create domain Files - START ***************************");

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Create Domain File - START", IntelliConstantMessage.INFO);

                //1. build Entities - lookup list only
                StringBuilder sbEntities = new StringBuilder();

                // Here we get out of data folder 
                string vFolderPath = vBuildActivityModelObj.GenerateModelPath.Replace("data", string.Empty);
                //if (File.Exists(vFolderPath + "domain.yml"))
                //    File.Delete(vFolderPath + "domain.yml");

                if (File.Exists(vFolderPath + "domain.yml"))
                {
                    //File.Delete(vFolderPath + "domain.yml");

                    List<string> lines = File.ReadLines(vFolderPath + "domain.yml").ToList();

                    int ActionIndex = lines.IndexOf("actions:");
                    int EntitieIndex = lines.IndexOf("entities:");
                    int IntentIndex = lines.IndexOf("intents:");
                    int sessionIndex = lines.IndexOf("session_config:");
                    int SlotIndex = lines.IndexOf("slots:");

                    List<string> actions = lines.Take(EntitieIndex).ToList();
                    List<string> Entities = lines.Skip(EntitieIndex).Take(IntentIndex - EntitieIndex).ToList();
                    List<string> Intents = lines.Skip(IntentIndex).Take(sessionIndex - IntentIndex).ToList();
                    List<string> Slots = lines.Take(SlotIndex).ToList();

                    foreach (var item in Entities)
                    {
                        string Entitie = item.Replace("- ", "");

                        if (Entitie.ToUpper() != "LANGCODE" && Entitie.ToUpper() != "ENTITIES:" && Entitie != "")
                        {
                            if (!_htLookups.ContainsValue(Entitie))
                            {
                                // Here we add lookups in Hashtable 
                                _htLookups.Add(Entitie, Entitie);
                            }
                        }
                    }

                    foreach (var intent in Intents)
                    {
                        string strIntent = intent.Replace("- ", "");

                        if (strIntent.ToUpper() != "INTENTS:" && strIntent.ToUpper() != "FALLBACKINTENT" && strIntent != "")
                        {
                            if (!_htIntents.ContainsValue(strIntent.ToString()))
                            {
                                // Here we add lookups in Hashtable 
                                _htIntents.Add(strIntent.ToString(), strIntent.ToString());
                            }
                        }
                    }

                }

                // Here we order htlookups
                var orderedKeys = _htLookups.Keys.Cast<string>().OrderBy(c => c);

                sbEntities.AppendLine("entities:");

                sbEntities.AppendLine("- LangCode");

                // here we add htlookup into stringbuilder
                foreach (String k in orderedKeys)
                {
                    sbEntities.AppendLine("- " + k);
                }

                //2. build list of Intents
                StringBuilder sbIntentList = new StringBuilder();

                // Here we Order Intent 
                orderedKeys = _htIntents.Keys.Cast<string>().OrderBy(c => c);
                sbIntentList.AppendLine("intents:");

                // Here we add all intent into String builder
                foreach (String k in orderedKeys)
                {
                    //IntentAO
                    //Here we check Model contains AO Intent 
                    if (k.ToUpper().Contains("INTENTAO"))
                        AOIntentExist = true;

                    sbIntentList.AppendLine("- " + k);
                }

                // Here we add fallback intent in String Builder
                sbIntentList.AppendLine("- FallbackIntent");

                //3. build list of slots
                StringBuilder sbSlots = new StringBuilder();

                // Here we sort lookup data
                orderedKeys = _htLookups.Keys.Cast<string>().OrderBy(c => c);
                string strDomainSlots;

                sbSlots.AppendLine("slots:");

                strDomainSlots = "  LangCode:\n    initial_value: " + vBuildActivityModelObj.BuildLang + "\n";
                strDomainSlots = strDomainSlots + "    type: text";
                sbSlots.AppendLine(strDomainSlots);

                // here we add lookup data into StringBuilder
                foreach (String k in orderedKeys)
                {

                    strDomainSlots = "  " + k + ":" + "\n";
                    strDomainSlots = strDomainSlots + "    type: text";

                    sbSlots.AppendLine(strDomainSlots);
                }

                strDomainSlots = "  requested_slot:\n";
                strDomainSlots = strDomainSlots + "    type: unfeaturized";
                sbSlots.AppendLine(strDomainSlots);

                string temp = string.Empty;
                //Here we check Model contains AO Intent 
                if (AOIntentExist == true)
                {
                    temp = @"actions:
- Action_Intent
- Action_Conversational_Intent
- Action_Reset_Slot
- Fallback_Action";

                }
                else
                {
                    temp = @"actions:
- Action_Intent
- Action_Reset_Slot
- Fallback_Action";

                }

                sbReturnValue.AppendLine(temp);
                sbReturnValue.AppendLine(sbEntities.ToString());
                sbReturnValue.AppendLine(sbIntentList.ToString());

                temp = @"session_config:
  carry_over_slots_to_new_session: true
  session_expiration_time: 0";

                sbReturnValue.AppendLine(temp);
                sbReturnValue.AppendLine(sbSlots.ToString());
                temp = @"  requested_slot:
    type: unfeaturized";

                // Here we Create domain file 
                File.WriteAllText(vFolderPath + "domain.yml", sbReturnValue.ToString());

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Create Domain File. - END", IntelliConstantMessage.INFO);

                Console.WriteLine("*************** Create domain Files - END ***************************");


                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);
            }
            catch (Exception ex)
            {
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + ": Error Occured"
                                , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                                , IntelliConstantMessage.SERVER);

                // here we Insert Activity Log
                BondUtility.InsertActivityDetails(vBuildActivityModelObj, "Error in Create domain file : " + ex.Message + "Inner exception:" + ex.InnerException.Message, IntelliConstantMessage.ERROR);

                throw;
            }
        }

        /// <summary>
        /// read NLU in Objects 
        /// </summary>
        /// <param name="strNLUData"></param>
        /// <returns></returns>
        public List<sbIntent> GetNLUDataInObjects(string strNLUData)
        {
            StringBuilder returnVar = new StringBuilder();
            string intentName = string.Empty;

            bool isIntentStarted = false;
            int lengthOfText = 0;
            int lookCnt = 0;
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            sbIntent objIntent = new sbIntent();
            List<sbIntent> lstIntentCollection = new List<sbIntent>();
            List<sbLookupsForUttarence> lstLookups = new List<sbLookupsForUttarence>();
            List<sbUtterence> lstUtters = new List<sbUtterence>();

            try
            {
                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                Console.WriteLine("*************** Get File Data Into String Builder - START ***************************");

                // Here we split string by \n
                string[] lines = strNLUData.Split('\n');

                foreach (string line in lines)
                {
                    // Here we remove whitespaces from line
                    string lineData = line.Trim();
                    lengthOfText = line.TrimEnd().Length;

                    // Here we check Line Length is > 0
                    if (lengthOfText > 0)
                    {
                        // Here we Check First Two Charecter of line contains ## if they match then set flag false
                        if (lineData.ToUpper().Substring(0, 2) == "##".ToUpper())
                        {
                            isIntentStarted = false;
                        }

                        if (lengthOfText > 10)
                        {
                            // Here we starting of new intent
                            if (lineData.ToUpper().Substring(0, 10) == "## intent:".ToUpper())
                            {
                                if (!isIntentStarted) lstIntentCollection.Add(objIntent);

                                isIntentStarted = true;

                                lstUtters = new List<sbUtterence>();
                                objIntent = new sbIntent();
                                lengthOfText = lineData.Length;

                                // Here we Set IntentName 
                                intentName = lineData.Substring(10, lengthOfText - 10);
                                objIntent.IntentName = intentName;

                                //Console.WriteLine("************ IntentName: " + objIntent.IntentName + "***********\n");

                            }
                        }

                        // This is like we are iterating through utterences.
                        if (lineData.ToUpper().Substring(0, 2) == "- ".ToUpper() && isIntentStarted)
                        {
                            // Here we check Utternace Contains LookUp(slot) table
                            if (lineData.Contains("[") || lineData.Contains("("))
                            {
                                lookCnt = 0;
                                lstLookups = new List<sbLookupsForUttarence>();

                                string results = string.Empty;

                                for (int i = 0; i < lineData.Length; i++)
                                {
                                    if (!lineData.Contains("[") || !lineData.Contains("("))
                                    {
                                        break;
                                    }
                                    // Potentially add error checking in case the file doesn't have []
                                    // Here we find index of Lookup "(" and ")"
                                    int indexOfOpenParen = lineData.IndexOf('(', 0);
                                    int indexOfCloseParen = lineData.IndexOf(')', indexOfOpenParen + 1);

                                    // Here we Get LookUp Name
                                    string lookUpName = lineData.Substring(indexOfOpenParen + 1, indexOfCloseParen - indexOfOpenParen - 1);

                                    // Here we Find the index of lookup value
                                    int indexOfOpenSqParen = lineData.IndexOf('[');
                                    int indexOfCloseSqParen = lineData.IndexOf(']', indexOfOpenSqParen + 1);

                                    // Here we get lookup value
                                    string lookUpVal = lineData.Substring(indexOfOpenSqParen + 1, indexOfCloseSqParen - indexOfOpenSqParen - 1);

                                    // Check Lookup value and name is not null
                                    if (lookUpName != "" && lookUpVal != "")
                                    {
                                        // Create an object of lookuputternaces
                                        sbLookupsForUttarence objLookUp = new sbLookupsForUttarence();

                                        objLookUp.LookupName = lookUpName;
                                        objLookUp.LookupVal = lookUpVal;
                                        objLookUp.LookupCnt = lookCnt;

                                        // Here we add lookup value and name into Lookup List
                                        lstLookups.Add(objLookUp);
                                        lookCnt = lookCnt + 1;
                                    }

                                    // Here we check Index of open '(' and remove braces from line
                                    if (indexOfOpenParen != -1)
                                        results = lineData.Substring(0, indexOfOpenParen) + lineData.Substring(indexOfCloseParen + 1);

                                    // here we replace lookup tables into value.
                                    results = results.Replace("[" + lookUpVal + "]", lookUpVal);

                                    // here we assign modifed line to linedata
                                    lineData = results;

                                }  // this is for entire one line of uttrance

                                sbUtterence objUtter = new sbUtterence();
                                objUtter.Uttarence = lineData;
                                objUtter.Lookups = lstLookups;

                                lstUtters.Add(objUtter);

                                //Console.WriteLine("\t" + lineData);

                                lineData.Trim();
                                lengthOfText = lineData.TrimEnd().Length;
                            }
                            else
                            {
                                lstLookups = new List<sbLookupsForUttarence>();
                                sbUtterence objUtter = new sbUtterence();

                                objUtter.Uttarence = lineData;
                                objUtter.Lookups = lstLookups;

                                lstUtters.Add(objUtter);
                            }
                            objIntent.UtterencesList = lstUtters;

                        }  //this is where normal line is over.

                    }  //This is for empty line validation
                }    //New line is taken for processing

                lstIntentCollection.Add(objIntent);

                Console.WriteLine("*************** Get File Data Into String Builder - END ***************************");

                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                return lstIntentCollection;
            }
            catch (Exception)
            {
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + ": Error Occured", IntelliConstantMessage.LOGGING_LEVEL_ONE
                                , IntelliConstantMessage.ERROR, IntelliConstantMessage.SERVER);

                throw;
            }
        }

        /// <summary>
        /// Generate hash tables for NLU, Domain and Stories - Common method
        /// </summary>
        /// <param name="lstIntentCollection"></param>
        /// <returns></returns>
        public void PopulateHashTables(List<sbIntent> lstIntentCollection)
        {
            // Here we Get Method Name for Log
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                Console.WriteLine("*************** Populate Data Into HashTable - START ***************************");

                StringBuilder sb = new StringBuilder();

                // This hashtable for hold storywise lookup table data
                Hashtable lstStorywisehtLookups = new Hashtable();

                // Here we Check lstIntentCollection one by one
                foreach (sbIntent it in lstIntentCollection)
                {
                    if (it.IntentName != "")
                    {
                        int storyCntr = 1;
                        string strIntentName = it.IntentName;

                        // Here we check intent Already exist in hashtable Intent
                        if (!_htIntents.ContainsValue(strIntentName))
                        {
                            if (strIntentName.ToUpper() == "INTENTAOACCOUNTOPENING" || strIntentName.ToUpper() == "INTENTAOHELPTOCHOOSE")
                            {
                                // Here we add new intent into hashtable
                                _htIntents.Add(strIntentName, strIntentName);

                                string storyName = "## " + strIntentName + " story 00\n";
                                string storyContent = "* " + strIntentName + "\n  - Action_Conversational_Intent\n\n";
                                _htStories.Add(storyName, storyContent);
                            }
                            else
                            {
                                // Here we add new intent into hashtable
                                _htIntents.Add(strIntentName, strIntentName);

                                string storyName = "## " + strIntentName + " story 00\n";

                                string storyContent = "* " + strIntentName + "\n  - Action_Intent\n\n";
                                _htStories.Add(storyName, storyContent);
                            }


                        }

                        // Here we check utternaces of all utterances of intent
                        foreach (sbUtterence ut in it.UtterencesList)
                        {
                            if (ut.Lookups.Count > 0)
                            {
                                // Here we Create Story Title
                                string storyName = "## " + strIntentName + " story 00" + storyCntr + " \n";
                                string storyContent = "";
                                string slotPart = "";
                                string storyWiseLkpTables = string.Empty;

                                // Here we Create Story Name
                                storyContent = "* " + strIntentName + " {";

                                foreach (sbLookupsForUttarence lfu in ut.Lookups)
                                {
                                    // Here we Create Story Content
                                    storyContent = storyContent + "\"" + lfu.LookupName.ToString() + "\":\"" + lfu.LookupVal + "\", ";

                                    // Here we Create Story Slots
                                    slotPart = slotPart + "    - slot{\"" + lfu.LookupName.ToString() + "\": \"" + lfu.LookupVal + "\"} \n";

                                    // Here we Add All LookupNames into String
                                    storyWiseLkpTables = storyWiseLkpTables + ", " + lfu.LookupName.ToString();

                                    if (lfu.LookupName.ToString().Contains(':'))
                                        lfu.LookupName = lfu.LookupName.Split(':')[0].ToString();

                                    if (!_htLookups.ContainsValue(lfu.LookupName.ToString()))
                                    {
                                        // Here we add lookups in Hashtable 
                                        _htLookups.Add(lfu.LookupName.ToString(), lfu.LookupName.ToString());
                                    }
                                }

                                // Here we Close Story
                                storyContent = storyContent.Substring(0, storyContent.Length - 2) + "} \n";

                                // Here we add Action in Story
                                if (strIntentName.ToUpper() == "INTENTAOACCOUNTOPENING" || strIntentName.ToUpper() == "INTENTAOHELPTOCHOOSE")
                                {
                                    storyContent = storyContent + "  - Action_Conversational_Intent \n\n";
                                }
                                else
                                    storyContent = storyContent + "  - Action_Intent \n\n";

                                // Here we Check Story Already exist or not
                                if (!_htStories.ContainsValue(storyContent))
                                {
                                    // Here we check All Slots of story already exist or not
                                    if (!lstStorywisehtLookups.ContainsValue(storyWiseLkpTables))
                                    {
                                        // Here we Add All Slots into lsthtLookups
                                        lstStorywisehtLookups.Add(storyWiseLkpTables, storyWiseLkpTables);

                                        // Here we Add new Story in HashTable
                                        _htStories.Add(storyName, storyContent);

                                        // Here we Increase Story Count by 1
                                        storyCntr = storyCntr + 1;
                                    }
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("*************** Populate Data Into HashTable - END ***************************");

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

        /// <summary>
        /// This method for Generate Lookup Files
        /// </summary>
        /// <param name="dtLkpData"></param>
        /// <param name="destinationFilePath"></param>
        public void GenerateLookupFiles(DataTable dtLkpData, string destinationFilePath)
        {
            // Here we Get Method Name for Log
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            string strLookupTableName = string.Empty;
            string strPrevLookupTableName = string.Empty;
            string strLookupTableValue = string.Empty;
            StringBuilder sbLkpFileData = new StringBuilder();

            try
            {

                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                Console.WriteLine("*************** Generate Lookups Files - START ***************************");

                DataView dv = dtLkpData.DefaultView;
                dv.Sort = "LookupTableName, LookupTableValue";

                // Here we add LookupData Folder
                destinationFilePath = destinationFilePath + "\\lookupData\\";

                //delete all lookup files before generating files
                DirectoryInfo baseDir = new DirectoryInfo(destinationFilePath);

                FileInfo[] files = baseDir.GetFiles();

                // This Loop for delete files
                foreach (FileInfo file in files)
                {
                    file.Delete();
                }

                if (!Directory.Exists(destinationFilePath))
                {
                    Directory.CreateDirectory(destinationFilePath);

                }

                foreach (DataRow dr in dv.ToTable().Rows)
                {
                    strPrevLookupTableName = strLookupTableName;
                    strLookupTableName = dr["LookupTableName"].ToString().Trim();
                    strLookupTableValue = dr["LookupTableValue"].ToString().Trim();

                    if (strLookupTableName == strPrevLookupTableName)
                    {
                        sbLkpFileData.AppendLine(" " + strLookupTableValue);
                    }
                    else
                    {

                        if (strLookupTableName != "" && sbLkpFileData.ToString() != "")
                            File.WriteAllText(destinationFilePath + strPrevLookupTableName + ".txt", sbLkpFileData.ToString());

                        sbLkpFileData = new StringBuilder();
                        if (strLookupTableValue != "") sbLkpFileData.AppendLine(" " + strLookupTableValue);
                    }
                }

                if (strLookupTableName != "" && sbLkpFileData.ToString() != "")
                    File.WriteAllText(destinationFilePath + strPrevLookupTableName + ".txt", sbLkpFileData.ToString());

                Console.WriteLine("*************** Generate Lookups Files - END ***************************");

                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);
            }
            catch //(Exception)
            {
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + ": Error Occured"
                           , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                           , IntelliConstantMessage.SERVER);

                throw;
            }
        }

        /// <summary>
        /// This Method for Get lemmatize sentance
        /// </summary>
        /// <param name="vSentence"></param>
        /// <returns></returns>
        public static List<string> GetLemmaSentace(List<string> vSentence)
        {
            // Here we Get Method Name for Log
            string strMethodName = MethodBase.GetCurrentMethod().Name;

            try
            {
                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONSTART
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                string sentance = JsonConvert.SerializeObject(vSentence);

                var client = new RestClient(_lemmaEndPoint + "/Lemmatize");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", _lemmaAuth);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", "{\r\n    \"message\":   " + sentance + "\r\n}", ParameterType.RequestBody);
                //request.AddParameter("application/json", "{\r\n    \"message\": \"" + sentances + "\r\n}", ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                //Console.WriteLine(response.Content);

                LemmaResponse lstStrResponse = JsonConvert.DeserializeObject<LemmaResponse>(response.Content);

                //Console.WriteLine(lstStrResponse.response);

                // The following code is used for User Logging
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + IntelliConstantMessage.EXECUTIONEND
                                            , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.INFO
                                            , IntelliConstantMessage.SERVER);

                return lstStrResponse.response;

            }
            catch (Exception)
            {
                IntelliLogDAL.WriteLogs(_pageName, strMethodName, strMethodName + ": Error Occured"
                          , IntelliConstantMessage.LOGGING_LEVEL_ONE, IntelliConstantMessage.ERROR
                          , IntelliConstantMessage.SERVER);

                throw;
            }



        }

    }

    public class LemmaResponse
    {
        public List<string> response { get; set; }
        public int statusCode { get; set; }
    }
}
