var expiresCount = 830;

window.addEventListener('popstate', function (event) {
	CallToAction(window.location.href);
});

async function Refresh()
{
  var tokenStatus = await TokenStatus();

  if(!tokenStatus.refreshToken || !tokenStatus.exchangeRefreshToken)
  {
    console.warn("Tokens are not setup, redirecting to complete bootstrap process..");
    CallToAction('/setup/initial');
  }

  if(null==tokenGlobal)
  {
    await SignIn();
  }

  document.getElementById('tenantsDropdownButton').disabled=true;
  const [] = await Promise.allSettled([
		ProfileRefresh(), 
		TenantRefresh()
	]);

  document.getElementById('tenantsDropdownButton').disabled=false;

  setInterval(Heartbeat, 120000);
}

async function BootstrapRefresh()
{
  var graphUrl = await GraphTokenUrl();
  if(null != graphUrl.url && graphUrl.url != 'undefined')
  {
    document.getElementById("tokenLink").href=graphUrl.url;
  }
  BootstrapTokenCheck();
  setInterval(BootstrapTokenCheck, 1337);
}

async function BootstrapTokenCheck()
{
  var tokenStatus = await TokenStatus();

  if (tokenStatus.refreshToken) {
    if(document.getElementById("grtIcon").classList.contains("bi-slash-square"))
    {
      document.getElementById("grtIcon").classList.remove("bi-slash-square");
      document.getElementById("grtIcon").classList.add("bi-check-square");
      document.getElementById("grtContainer").classList.remove("bg-secondary");
      document.getElementById("grtContainer").classList.add("bg-success");
      document.getElementById("instructions").innerHTML='<div class="spinner-border text-warning" role="status"><span class="visually-hidden">Loading...</span></div>';
      var device = await ExchangeTokenUrlCode()
      if(null != device.url && device.url != 'undefined')
      {
        document.getElementById("instructions").innerHTML=`Sign in <a id="tokenLink" target="_blank" href="${device.url}" class="a-general-dark">HERE</a> as your Global Admin using code <span id="deviceCode" class="alt-text-dark">${device.code}</span></h5>`
        expiresCount= device.expires-30;
        setInterval(ExpireCount, 1000);
      }
    }
  }

  if (tokenStatus.exchangeRefreshToken) {
    document.getElementById("ertIcon").classList.remove("bi-slash-square");
    document.getElementById("ertIcon").classList.add("bi-check-square");
    document.getElementById("ertContainer").classList.remove("bg-secondary");
    document.getElementById("ertContainer").classList.add("bg-success");
    document.getElementById("ertIcon").classList.remove("bi-x-square");
    document.getElementById("ertContainer").classList.remove("bg-danger");
  }

  if(tokenStatus.refreshToken && tokenStatus.exchangeRefreshToken)
  {
    document.getElementById("instructions").innerText="Completed";
    document.getElementById("instructions").classList.add("text-success");
    window.location="/";
  }
}

async function ExpireCount()
{
  expiresCount=expiresCount-1;
  console.warn(`Device Code expires in: ${expiresCount}`);

  if (expiresCount<=120)
  {
    document.getElementById("expireCard").classList.remove("d-none");
    if(expiresCount>0)
    {
      document.getElementById("expireCount").innerText=expiresCount;
    }
    else
    {
      document.getElementById("expireCount").innerText="Expired";
      document.getElementById("expireCount").classList.remove("ms-2");
      document.getElementById("expireHeading").innerHTML=document.getElementById("expireCount").outerHTML;
      document.getElementById("instructionsCard").classList.add("d-none");
      document.getElementById("ertIcon").classList.remove("bi-slash-square");
      document.getElementById("ertIcon").classList.add("bi-x-square");
      document.getElementById("ertContainer").classList.remove("bg-secondary");
      document.getElementById("ertContainer").classList.add("bg-danger");
    }
  }
}

async function TenantRefresh()
{
  document.getElementById('tenantData').innerHTML='';
  const tenantJson = await GetTenants(true);
  var dropItems="";
  for (var i = 0; i < tenantJson.length; i++){
      dropItems+=`<li><a style='border-top: none' class="onclick-highlight panel-section-dark dropdown-item" data-tenant="${tenantJson[i].defaultDomainName}" data-customerid="${tenantJson[i].customerId}" onclick="SelectOption('tenantFilter',this.innerText,this.dataset.tenant, this.dataset.customerid, true)">${tenantJson[i].displayName}</a></li>`; 
  }
  document.getElementById('tenantData').innerHTML=dropItems;
}

async function ProfileRefresh()
{
  document.getElementById('profileDropdownButton').disabled=true;
  const profile = await AuthMe();
  SelectOption('tenantFilter', profile.clientPrincipal.lastTenantName,profile.clientPrincipal.lastTenantDomainName,profile.clientPrincipal.lastTenantCustomerId);
  document.getElementById(`table${profile.clientPrincipal.defaultPageSize.toString()}`).classList.add('toggle-button-active');
  document.getElementById('profileDropdownButton').disabled=false;
  if (profile.clientPrincipal.photoData != '')
  {
    document.getElementById("profileDropdownButton").style=`background-size:100%;background-image:url(data:image/jpg;base64,${profile.clientPrincipal.photoData})`;
  }
  else
  {
    try
    {
      document.getElementById("profileDropdownButton").style.backgroundImage=`url(${GenerateAvatar(profile.clientPrincipal.name.split(' ')[0].charAt(0)+profile.clientPrincipal.name.split(' ')[1].charAt(0),'#000000','#ffc107')})`;
    }
    catch
    {
      document.getElementById("profileDropdownButton").style.backgroundImage=`url(${GenerateAvatar(profile.clientPrincipal.name.charAt(0),'#000000','#ffc107')})`;
    }
    document.getElementById("profileDropdownButton").style.backgroundSize='100%';
  }
  document.getElementById("profileName").innerText=profile.clientPrincipal.name;
  document.getElementById("profileEmail").innerText=profile.clientPrincipal.userDetails;
  document.getElementById("profileRole").innerText=profile.clientPrincipal.userRoles[0].charAt(0).toUpperCase()+profile.clientPrincipal.userRoles[0].slice(1);
  document.getElementById("profileDropdown").dataset.tablesize='table'+profile.clientPrincipal.defaultPageSize.toString();
  document.getElementById("profileDropdown").dataset.userid=profile.clientPrincipal.userId;
  document.getElementById("profileDropdown").dataset.defaultUsage=profile.clientPrincipal.defaultUseageLocation;
}

function GenerateAvatar(text, foregroundColor, backgroundColor) {
  const canvas = document.createElement("canvas");
  const context = canvas.getContext("2d");

  canvas.width = 200;
  canvas.height = 200;

  // Draw background
  context.fillStyle = backgroundColor;
  context.fillRect(0, 0, canvas.width, canvas.height);

  // Draw text
  context.font = "bold 100px Arial";
  context.fillStyle = foregroundColor;
  context.textAlign = "center";
  context.textBaseline = "middle";
  context.fillText(text, canvas.width / 2, canvas.height / 1.9);

  return canvas.toDataURL("image/png");
}

function ProfileButtonClick(e, value)
{
  [].slice.call(document.getElementById('pageSizeGroup').children).forEach(element => element.classList.remove('toggle-button-active'));
  document.getElementById("profileDropdown").dataset.tablesize=`table${value}`;
  document.getElementById(`table${value}`).classList.add('toggle-button-active');
  EditUserProfile();
  //e.stopPropagation();
}

function EditUserProfile()
{
  try
  {
  EditProfile(document.getElementById("profileDropdown").dataset.userid,document.getElementById('tenantFilter').innerText,document.getElementById('tenantFilter').dataset.tenant,document.getElementById('tenantFilter').dataset.tenant,parseInt(document.getElementById("profileDropdown").dataset.tablesize.replace('table','')));
  }
  catch(error)
  {
    console.error(error);
  }
}

function ProfileDropdownButtonClick()
{
  document.getElementById(document.getElementById("profileDropdown").dataset.tablesize).focus();
}

function DropdownButtonClick(dropdownId,searchInputId) {
    document.getElementById(searchInputId).value='';
    DropdownFilterFunction(dropdownId, searchInputId);
}

function SelectOption(dropdownButtonId, option='', defaultDomain='', custid='', updateProfile=false)
{
    document.getElementById(dropdownButtonId).innerText=option.replace('*All','All').replace('* All','All');
    document.getElementById(dropdownButtonId).dataset.tenant=defaultDomain;
    document.getElementById(dropdownButtonId).dataset.customerid=custid;

    if(updateProfile)
    {
      EditUserProfile();
    }
}
  
function DropdownFilterFunction(dropdownId, searchInputId) {
    var input, filter, ul, li, a, i;
    input = document.getElementById(searchInputId);
    filter = input.value.toUpperCase();
    div = document.getElementById(dropdownId);
    a = div.getElementsByTagName("a");
    for (i = 0; i < a.length; i++) {
      txtValue = a[i].textContent || a[i].innerText;
      if (txtValue.toUpperCase().indexOf(filter) > -1) {
        a[i].style.display = "";
      } else {
        a[i].style.display = "none";
      }
    }
}

async function SidebarCollapse() {
  if(document.getElementById("sidebar").style.width != "250px")
  {
    document.getElementById("sidebar").style.width = "250px";
    document.getElementById("main").style.marginLeft = "250px";
  }
  else
  {
    document.getElementById("sidebar").style.width = "0";
    document.getElementById("main").style.marginLeft= "0";
  }
}

async function Heartbeat()
{
  var heartbeat = await GetHeartbeat();
  console.info(`API Heartbeat: ${JSON.stringify(heartbeat)}`);

}

async function LoadUrl(url)
{
  const nextTitle = 'FFTP - NEXT';
  const nextState = { additionalInformation: 'Updated the URL with JS' };
  window.history.pushState(nextState, nextTitle, url);
  CallToAction(url)
}

async function CallToAction(currentUrl)
{
  switch(currentUrl) {
    case '/setup/initial' || '/setup/initial/':
      window.location.replace('/setup/initial');
      break;
    default:
      // code block
  }
}
