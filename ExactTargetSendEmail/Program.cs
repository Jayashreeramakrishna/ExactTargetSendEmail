using ExactTargetSendEmail.ExactTargetService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace ExactTargetSendEmail
{
    public class Program
    {
        Screen screen = new Screen();
        string accountId = "youracountId";
        string UserName = "Username";
        string Password = "Password";
        string soapAPIEndPoint = "https://YOUR_SUBDOMAIN.soap.marketingcloudapis.com/Service.asmx";
        SoapClient framework = null;
        public static void Main(string[] args)
        {
            Program program = new Program();

            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Name = "UserNameSoapBinding";
            binding.Security.Mode = BasicHttpSecurityMode.TransportWithMessageCredential;
            binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
            binding.ReceiveTimeout = new TimeSpan(0, 5, 0);
            binding.OpenTimeout = new TimeSpan(0, 5, 0);
            binding.CloseTimeout = new TimeSpan(0, 5, 0);
            binding.SendTimeout = new TimeSpan(0, 5, 0);
            // Set the transport security to UsernameOverTransport for Plaintext usernames
            EndpointAddress endpoint = new EndpointAddress(program.soapAPIEndPoint);
            // Create the SOAP Client (and pass in the endpoint and the binding)


            program.framework = new SoapClient(binding, endpoint);
            program.framework.ClientCredentials.UserName.UserName = program.UserName;
            program.framework.ClientCredentials.UserName.Password = program.Password;

            
            program.StartSend();
        }

        public void StartSend()
        {
            
            string partnerKey = accountId  + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            string listFilePath = "your path";
            GetAddressBookFieldInfo(listFilePath);
            // Create the email
            var createEmailResult = CreateEmail(partnerKey);

            // Create a Data Extension
            var dataExtension = CreateDataExtension(partnerKey);

            // Convert email list to CSV and FTP to ExactTarget
            var exactTargetFileName = "yourfilename.csv";


            // Create an import definition to link the CSV file to the email
            CreateResult[] importResults = null;
            var importDef = CreateImportDefinition(partnerKey, dataExtension, exactTargetFileName, out importResults);

            // Load data into the Data Extention
            LoadDataIntoDataExtension(importDef, importResults);

            // Create Send Definition
            var definition = CreateEmailSendDefinition(partnerKey, "Test", createEmailResult);

            // Send the email
            ETPerformResult results = StartEmailSendDefinition(Convert.ToInt32(accountId), definition);

            if (!results.Status.Equals("OK") || string.IsNullOrEmpty(results.TaskID))
                throw new Exception("Failed to create SendEmail " + results.StatusMessage);
        }

        public void GetAddressBookFieldInfo(string EmailListXml)
        {
            //Program.screen = new Screen();

            screen.ID = 1; //address book
            screen.PageIndex = 1;
            screen.NoOfRecordsPerPage = 100;
            screen.SortingColumn = string.Empty;
            screen.SortingDirection = string.Empty;

            //############ 2-19-2010: wli revision ------------------------------
            //based on xml in emaillist field, not to based on db
            XmlDocument oXmlDoc = new XmlDocument();
            oXmlDoc.Load(EmailListXml);

            XmlNode oNode = oXmlDoc.DocumentElement;
            oNode = oNode.SelectSingleNode("/orderlineshippingaddresses/address");
            XmlNodeList oNodeList = oNode.ChildNodes;
            foreach (XmlNode node in oNodeList)
            {
                ScreenDetails sd = new ScreenDetails();

                sd.FieldName = node.Name;
                sd.IsVisible = true;
                sd.IsRequired = false;
                sd.IsEnable = true;

                if (sd.FieldName.ToLower().Equals("email1"))
                    sd.IsRequired = true;

                screen.ScreenDetailsCollection.Add(sd);
            }

        }

        public ETCreateResult CreateEmail(string partnerKey)
        {
            //#########################
            //create ET email
            //etManager
            Email email = new Email();

            email.CharacterSet = "UTF-8";
            email.Name = "Test";
            email.Subject = "Testing ET Sender Profile";



            // Always add Doctype
            email.HTMLBody = "<!doctype html> Hello world!";
            email.PartnerKey = partnerKey;

            var results = CreateEmail(Convert.ToInt32(accountId), email);

            if (!results.Status.Equals("OK") || results.CreatedItemID.Equals("0"))
                throw new Exception("Failed to create email: " + results.StatusMessage);

            return results;
        }
        public ETCreateResult CreateEmail(int iClientID, Email email)
        {
            var requestId = string.Empty;
            var emailID = string.Empty;
            var status = string.Empty;

            Portfolio portfolioObj = new Portfolio();

            email.IsHTMLPaste = true;
            email.IsHTMLPasteSpecified = true;
            APIObject[] apiObjects = { email };

            //#########################-----------
            // Only for sub-accounts using parent authentication
            ClientID clientID = new ClientID();
            clientID.ID = iClientID;
            clientID.IDSpecified = true;

            email.Client = clientID;
            //#########################-----------


            CreateResult[] results = framework.Create(new CreateOptions(), apiObjects, out requestId, out status);

            if (results != null && results.Any())
            {
                emailID = results[0].NewID.ToString();
            }

            var etResults = new ETCreateResult(requestId, emailID, status, results[0].StatusMessage, results);

            return etResults;
        }
        public DataExtension CreateDataExtension(string partnerKey)
        {
            DataExtension dataExtension = new DataExtension();
            dataExtension.Name = partnerKey;
            dataExtension.CustomerKey = partnerKey;

            List<DefaultValues> listDefaults = new List<DefaultValues>();

            DefaultValues ETSenderName = new DefaultValues();
            ETSenderName.CustomerKey = "SenderName";
            ETSenderName.DefaultValue = "TestSender";

            listDefaults.Add(ETSenderName);

            DefaultValues fromEmailID = new DefaultValues();
            fromEmailID.CustomerKey = "FromEmailID";
            fromEmailID.DefaultValue = "default email address here";

            listDefaults.Add(fromEmailID);

            //listDefaults.Add(replyEmailID);

            var results = createDataExtensionElateral(Convert.ToInt32(accountId), dataExtension,
                screen.ScreenDetailsCollection, listDefaults);

            if (!results.Status.Equals("OK"))
                throw new Exception("Failed to create Data Extension: " + results.StatusMessage);

            return dataExtension;
        }

        public ETCreateResult createDataExtensionElateral(int iClientID, DataExtension dataExtension,
            List<ScreenDetails> screenDetailsCollection, List<DefaultValues> listDefaults)
        {
            List<DataExtensionField> listDEFields = new List<DataExtensionField>();
            var requestId = string.Empty;
            var status = string.Empty;

            foreach (ScreenDetails sdField in screenDetailsCollection)
            {
                if (sdField.IsVisible)
                {
                    bool isRequired = false;
                    bool isRequiredSpecified = false;
                    if (sdField.IsRequired)
                    {
                        isRequired = true;
                        isRequiredSpecified = true;
                    }

                    DataExtensionFieldType defType = DataExtensionFieldType.Text;
                    if (sdField.FieldName.ToLower().Equals("email1"))
                    {
                        defType = DataExtensionFieldType.EmailAddress;
                    }

                    setDEField(ref listDEFields, sdField.FieldName, sdField.FieldName,
                        isRequired, isRequiredSpecified, defType, ref listDefaults);
                }
            }

            int iSendableDataExtensionFieldIndex = listDEFields.Count;

            //##############-----------------------------------------
            //SubscriberKey ( defined in WC\Web.config) 
            //SendableDataExtensionField
            setDEField(ref listDEFields, "SubscriberKey", "SubscriberKey", true, true, DataExtensionFieldType.Text, ref listDefaults);
            //##############-----------------------------------------

            setDEField(ref listDEFields, "SenderName", "SenderName", true, true, DataExtensionFieldType.Text, ref listDefaults);
            setDEField(ref listDEFields, "FromEmailID", "FromEmailID", true, true, DataExtensionFieldType.Text, ref listDefaults);
            setDEField(ref listDEFields, "ReplyEmailID", "ReplyEmailID", true, true, DataExtensionFieldType.Text, ref listDefaults);

            

            dataExtension.SendableDataExtensionField = listDEFields[iSendableDataExtensionFieldIndex];

            ExactTargetSendEmail.ExactTargetService.Attribute attribute = new ExactTargetSendEmail.ExactTargetService.Attribute();

            attribute.Name = "Subscriber Key"; 
            dataExtension.SendableSubscriberField = attribute;

            dataExtension.IsSendable = true;
            dataExtension.IsSendableSpecified = true;

            dataExtension.Fields = new DataExtensionField[listDEFields.Count];
            for (int i = 0; i < listDEFields.Count; i++)
            {
                dataExtension.Fields[i] = listDEFields[i];
            }

            APIObject[] objects = { dataExtension };

            //#########################-----------
            // Only for sub-accounts using parent authentication
            ClientID clientID = new ClientID();
            clientID.ID = iClientID;
            clientID.IDSpecified = true;
            CreateOptions options = new CreateOptions();

            dataExtension.Client = clientID;

            //#########################-----------

            // If not sub-account
            //CreateOptions options = new CreateOptions();

            
            CreateResult[] results = framework.Create(options, objects, out requestId, out status);
            var etResults = new ETCreateResult(requestId, results[0].NewID.ToString(), status, results[0].StatusMessage, results);


            return etResults;

        }
        private void setDEField(ref List<DataExtensionField> listDEFields, string Name,
            string CustomerKey, bool IsRequired, bool IsRequiredSpecified,
            DataExtensionFieldType FieldType, ref List<DefaultValues> listDefaults)
        {
            DataExtensionField field = new DataExtensionField();
            field.Name = Name;
            field.CustomerKey = CustomerKey;
            field.IsRequired = IsRequired;
            field.IsRequiredSpecified = IsRequiredSpecified;
            field.FieldType = FieldType;

            if (FieldType == DataExtensionFieldType.EmailAddress)
                field.FieldTypeSpecified = true;

            if (listDefaults != null)
            {
                foreach (DefaultValues defaultValue in listDefaults)
                {
                    if (defaultValue.CustomerKey.Equals(CustomerKey))
                    {
                        field.DefaultValue = defaultValue.DefaultValue;
                        break;
                    }
                }
            }

            listDEFields.Add(field);
        }

        public ImportDefinition CreateImportDefinition(string partnerKey, DataExtension dataExtension, string etMailingListFileName, out CreateResult[] createResults)
        {
            ImportDefinition importDef = new ImportDefinition();
            importDef.Name = partnerKey;
            importDef.CustomerKey = partnerKey;
            importDef.Description = "ImportDefinition: " + partnerKey;
            importDef.AllowErrors = true;
            importDef.AllowErrorsSpecified = true;

            //Destination Object                
            importDef.DestinationObject = dataExtension;

            importDef.UpdateType = ImportDefinitionUpdateType.Overwrite;
            importDef.UpdateTypeSpecified = true;

            // Map fields (required)
            importDef.FieldMappingType = ImportDefinitionFieldMappingType.InferFromColumnHeadings;
            importDef.FieldMappingTypeSpecified = true;

            //use ftp.ETFilename to get ETFilename
            importDef.FileSpec = etMailingListFileName;
            importDef.FileTypeSpecified = true;

            // Specify the FileType
            importDef.FileType = FileType.CSV;

            var results = createImportDefinitionWithDataExtract(Convert.ToInt32(accountId), importDef, dataExtension);
            createResults = results.CreateResults;

            if (!results.Status.Equals("OK"))
                throw new Exception("Failed to create ImportDefinition: " + results.StatusMessage);

            return importDef;
        }

        public ETCreateResult createImportDefinitionWithDataExtract(int iClientID, ImportDefinition importDef, DataExtension dataExtension)
        {
            CreateResult[] results;
            var requestID = string.Empty;
            var status = string.Empty;

            importDef.DestinationObject = dataExtension;

            FileTransferLocation fileTransferLocation = new FileTransferLocation();

            //###################----------
            //don't change it! pre-defined! which means where to get uploaded list file
            fileTransferLocation.CustomerKey = "your FTP" /* DON'T CHANGE */;
            importDef.RetrieveFileTransferLocation = fileTransferLocation;
            //###################----------

            APIObject[] apiObjectList = { importDef };

            //#########################-----------
            // Only for sub-accounts using parent authentication
            ClientID clientID = new ClientID();
            clientID.ID = iClientID;
            clientID.IDSpecified = true;

            CreateOptions options = new CreateOptions();
            importDef.Client = clientID;
            //#########################-----------

            // If not sub-account
            //CreateOptions options = new CreateOptions();
            if (apiObjectList != null)
            {

            }

            
            results = framework.Create(options, apiObjectList, out requestID, out status);

            
            var etResults = new ETCreateResult(requestID, results[0].NewID.ToString(), status, results[0].StatusMessage, results);


            return etResults;
        }

        public void LoadDataIntoDataExtension(ImportDefinition importDef, CreateResult[] ImportResults)
        {
            //get the taskID
            var result = startImportDefinition(Convert.ToInt32(accountId), importDef);

            ////call et.retrieveImportResults by TaskID;  
            APIObject[] apiResults;
            ImportResultsSummary importResultsSummary = null;

            ////check to see if id.ImportStatus is done            
            int iCnt = 0;
            bool isDone = false;
            bool hasError = false;

            //defaults: 15 times
            while (iCnt < 30 && !isDone && !hasError)
            {
                ////call et.retrieveImportResults by TaskID
                apiResults = retrieveImportResults(Convert.ToInt32(accountId), result.TaskID); //taskID should be passed
                importResultsSummary = null;

                for (int cntr = 0; cntr < ImportResults.Length; cntr++)
                    importResultsSummary = (ImportResultsSummary)apiResults[cntr];

                if (importResultsSummary != null && !importResultsSummary.ImportStatus.ToUpper().Contains("ERROR"))
                {
                    //the import is done
                    //goto DE >> check DE >> View Data
                    if (importResultsSummary.ImportStatus.ToUpper().Equals("COMPLETED"))
                        isDone = true;
                }
                else
                    hasError = true;

                iCnt++;
                //defaults: sleep 5 sec (5000)
                Thread.Sleep(5000);
            }

            if (!isDone)
                throw new Exception($"The UploadEmailListToETAndAssociateItWithImportDef process is failed: {importResultsSummary.ImportStatus}");
        }

        public ETPerformResult startImportDefinition(int iClientID, ImportDefinition importDef)
        {
            var taskID = string.Empty;
            var requestID = string.Empty;
            var status = string.Empty;
            var statusMessage = string.Empty;

            PerformOptions options = new PerformOptions();

            //we have to sepcify this, otherwise importdefinition start will not work for sub-accounts
            //#########################-----------
            // Only for sub-accounts using parent authentication
            ClientID clientID = new ClientID();
            clientID.ID = iClientID;
            clientID.IDSpecified = true;
            options.Client = clientID;
            //#########################-----------           
            if (importDef != null)
            {
                //log.Debug("ET Request API Object Definitions for 'Perform (startImportDefinition)' Method :" + xmlSerializer.Serialize(new InteractionBaseObject[] { importDef }));
            }


            PerformResult[] results =
                framework.Perform(options, "start", new InteractionBaseObject[] { importDef }, out status, out statusMessage,
                                      out requestID);

            if (results != null && results.Length > 0)
            {
                if (results[0].Task != null)
                {
                    taskID = results[0].Task.ID;
                }
            }

            return new ETPerformResult(requestID, taskID, status, statusMessage, results);
        }

        /////
        //// Retrieves information for importDefinition. If number of erorrs == 0 then you application can
        //// decide to send emails or not.
        //// 
        public APIObject[] retrieveImportResults(int iClientID, string taskID)
        {
            var requestID = string.Empty;
            APIObject[] results = null;

            // Filter by TaskResultID (the unique id for the import)
            SimpleFilterPart sfp = new SimpleFilterPart();
            sfp.Property = "TaskResultID";
            sfp.SimpleOperator = SimpleOperators.equals;
            sfp.Value = new string[] { taskID };

            //// Create the RetrieveRequest
            RetrieveRequest request = new RetrieveRequest();

            //#########################-----------
            ClientID clientID = new ClientID();
            clientID.ID = iClientID;
            clientID.IDSpecified = true;
            request.ClientIDs = new ClientID[] { clientID };
            // Tells system to retrieve information from specified Sub-Account.
            //#########################-----------

            request.ObjectType = "ImportResultsSummary";
            request.Filter = sfp;
            request.Properties =
                new string[]
                    {
                        "ImportDefinitionCustomerKey", "ImportType", "ImportStatus", "ID", "ObjectID",
                        "NumberDuplicated", "NumberErrors", "NumberSuccessful", "DestinationID", "TaskResultID"
                    };

           
            // Execute the RetrieveRequest 
            framework.Retrieve(request, out requestID, out results);

            if (results != null)
            {
                //log.Debug("ET Request Results For 'Retrieve (retrieveImportResults)' Method:" + xmlSerializer.Serialize(results));
            }

            return results;
        }

        private EmailSendDefinition CreateEmailSendDefinition(string partnerKey, string emailName, ETCreateResult createEmailResult)
        {
            EmailSendDefinition definition = new EmailSendDefinition();

            definition.Name = partnerKey;
            definition.CustomerKey = partnerKey;

            SenderProfile senderProfile = new SenderProfile();
            senderProfile.CustomerKey = "Your senderprofile key";
            senderProfile.FromName = "TestET" + DateTime.Now;
            senderProfile.AutoReply = true;
            senderProfile.AutoReplySpecified = true;
            senderProfile.ReplyToDisplayName = "Reply Here";
            senderProfile.ReplyToAddress = "testemail1@test.com";
            definition.SenderProfile = senderProfile;


            var results = CreateEmailSendDefinition(Convert.ToInt16(accountId), definition);

            if (!results.Status.Equals("OK"))
                throw new Exception("Failed to create EmailSendDefinition: " + results.StatusMessage);

            return definition;
        }

        public ETCreateResult CreateEmailSendDefinition(int iClientID, EmailSendDefinition definition)
        {
            CreateResult[] results;
            var requestId = string.Empty;
            var status = string.Empty;

            Email email = new Email();
            email.Name = "Test";
            email.ID = 41496172; //email id from createemail response
            email.IDSpecified = true;
            definition.Email = email; //Associate Email with defintion.

            SendClassification sendclass = new SendClassification();
            sendclass.CustomerKey = "YOURSendClassificationkey"; //In UI goto ADMIN >> Send Management >> SendClassification
            definition.SendClassification = sendclass;

            definition.SendDefinitionList = new SendDefinitionList[1];
            definition.SendDefinitionList[0] = new SendDefinitionList();

            //get retrieve DE CustomObjectID
            DataExtension extension = retrieveDataExtension(iClientID, "Yourcustomerkey");

            definition.SendDefinitionList[0].CustomerKey = extension.CustomerKey;
            definition.SendDefinitionList[0].DataSourceTypeID = DataSourceTypeEnum.CustomObject;
            definition.SendDefinitionList[0].CustomObjectID = extension.ObjectID; //this GUID of DataExtension and required.
            definition.SendDefinitionList[0].DataSourceTypeIDSpecified = true;

            //################################################            
            //assign PUBLICATION_LIST here for UN-Subs
            definition.SendDefinitionList[0].List = new List();
            definition.SendDefinitionList[0].List.ID = 3958318; // >>my subscribers >> publication list
            definition.SendDefinitionList[0].List.IDSpecified = true;

            //#########################-----------
            // Only for sub-accounts using parent authentication
            ClientID clientID = new ClientID();
            clientID.ID = iClientID;
            clientID.IDSpecified = true;

            definition.Client = clientID;
            //#########################-----------           

            definition.IsMultipart = true;
            definition.IsMultipartSpecified = true;

            APIObject[] createObjects = { definition };

            results = framework.Create(new CreateOptions(), createObjects, out requestId, out status);

            return new ETCreateResult(requestId, results[0].NewID.ToString(), status, results[0].StatusMessage, results);
        }

        public DataExtension retrieveDataExtension(int iClientID, string customerKey)
        {
            var requestID = string.Empty;
            APIObject[] results;
            RetrieveRequest request = new RetrieveRequest();
            request.ObjectType = "DataExtension";
            request.Properties = new string[]
                            {
                                "ObjectID", "CustomerKey", "Name", "IsSendable", "SendableSubscriberField.Name"
                            };

            SimpleFilterPart filter = new SimpleFilterPart();
            filter.Property = "CustomerKey";
            filter.SimpleOperator = SimpleOperators.equals;
            filter.Value = new string[] { customerKey };
            request.Filter = filter;

            //#########################-----------
            ClientID clientID = new ClientID();
            clientID.ID = iClientID;
            clientID.IDSpecified = true;
            request.ClientIDs = new ClientID[] { clientID };
            // Tells system to retrieve information from specified Sub-Account.
            //#########################-----------
            
            string status = framework.Retrieve(request, out requestID, out results);

            DataExtension extension = null;
            if (results != null && results.Length > 0)
                extension = (DataExtension)results[0];
            else
                throw new Exception($"Failed to retrieve Data Extension: No APIObjects returned. Results array is {(results == null ? "null" : "empty")}");

            return extension;
        }

        public ETPerformResult StartEmailSendDefinition(int iClientID, EmailSendDefinition definition)
        {
            var taskID = string.Empty;
            var requestID = string.Empty;
            var status = string.Empty;
            var statusMessage = string.Empty;

            PerformOptions options = new PerformOptions();

            //#########################-----------
            //For only perform API call, ClientId property will go in Options.
            ClientID clientID = new ClientID();
            clientID.ID = iClientID;
            clientID.IDSpecified = true;

            options.Client = clientID;
            //#########################-----------
            //defining encoding style for email 
            //refer https://developer.salesforce.com/docs/atlas.en-us.noversion.mc-apis.meta/mc-apis/working_with_soap_web_service_api.htm for adding encoding to Soap ET API 
            definition.Email.CharacterSet = "UTF-8";
            APIObject[] objects = { definition };

            PerformResult[] results = framework.Perform(options, "start", objects, out status, out statusMessage, out requestID);


            if (results[0] != null) taskID = results[0].Task.ID;

            return new ETPerformResult(requestID, taskID, status, statusMessage, results);
        }

    }


    public class ETAPIResult
    {
        public string RequestID { get; set; }

        public string Status { get; set; }
        public string StatusMessage { get; set; }


        public ETAPIResult(string requestID, string status, string statusMessage)
        {
            this.RequestID = requestID;
            this.Status = status;
            this.StatusMessage = statusMessage;

        }
    }
    public class ETCreateResult : ETAPIResult
    {
        public string CreatedItemID { get; set; }
        public CreateResult[] CreateResults { get; set; }

        public ETCreateResult(string requestID, string createdItemID, string status, string statusMessage, CreateResult[] results)
            : base(requestID, status, statusMessage)
        {
            this.CreatedItemID = createdItemID;
            this.CreateResults = results;
        }
    }

    public class ETPerformResult : ETAPIResult
    {
        public string TaskID { get; set; }
        public PerformResult[] PerformResults { get; set; }

        public ETPerformResult(string requestID, string taskID, string status, string statusMessage, PerformResult[] results)
            : base(requestID, status, statusMessage)
        {
            this.TaskID = taskID;
            this.PerformResults = results;
        }
    }

    public class DefaultValues
    {
        public string CustomerKey = string.Empty;
        public string DefaultValue = string.Empty;
    }
}
