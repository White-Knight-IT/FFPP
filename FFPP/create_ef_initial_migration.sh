#!/bin/sh

# Create the initial DB migrations

dotnet ef migrations add InitialCreate_FfppLogsDbContext --context FfppLogsDbContext -o "./Migrations/FfppLogsDbContext_Migrations"
dotnet ef migrations add InitialCreate_ExcludedTenantsDbContext --context ExcludedTenantsDbContext -o "./Migrations/ExcludedTenantsDbContext_Migrations"
dotnet ef migrations add InitialCreate_UserProfilesDbContext --context UserProfilesDbContext  -o "./Migrations/UserProfilesDbContext_Migrations"

# Create the databases from latest migrations (read databases from update_databases.txt)

input="update_databases.txt"
while read -r line
do
  eval $line
done < "$input"

echo ""
echo "Script Done"
