# login
Login-AzureRmAccount

# choose appropriate subscription
Select-AzureRmSubscription -Tenant <tenantId>

# get registry creds
$registry = Get-AzureRmContainerRegistry -ResourceGroupName meetup -Name myMeetupRegistry
$creds = Get-AzureRmContainerRegistryCredential -Registry $reg

# docker login
$creds.Password | docker login $registry.LoginServer -u $creds.Username --password-stdin

# create image
docker-compose build

# tag & push
$tag = "$($registry.LoginServer)/sampleservice:latest"
docker tag sampleservice:latest $tag
docker push $tag
