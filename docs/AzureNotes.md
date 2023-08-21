Setting up managed identity for web and connecting with SQL DB: https://learn.microsoft.com/en-us/azure/azure-sql/database/azure-sql-dotnet-quickstart?view=azuresql&tabs=visual-studio%2Cpasswordless%2Cservice-connector%2Cportal#connect-the-app-service-to-azure-sql-database

az webapp connection create sql \
    -g mzikmund \
    -n app-mzikmund \
    --tg mzikmund \
    --server sql-mzikmund \
    --database sqldb-mzikmund \
    --system-identity