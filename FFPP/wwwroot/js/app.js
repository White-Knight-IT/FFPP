window.addEventListener('popstate', function (event) {
	CallToAction(window.location.href);
});

async function Refresh()
{
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

  const isSetupResponse = await IsSetup();
  if(!isSetupResponse.isSetup)
  {
    location.href="/setup/initial";
  }
}

async function TenantRefresh()
{
  document.getElementById('tenantData').innerHTML='';
  const tenantJson = await GetTenants(true);
  var dropItems="";
  for (var i = 0; i < tenantJson.length; i++){
      dropItems+=`<li><a style='border-top: none' class="onclick-highlight panel-section-dark dropdown-item modal-bg-dark" data-tenant="${tenantJson[i].defaultDomainName}" data-customerid="${tenantJson[i].customerId}" onclick="SelectOption('tenantFilter',this.innerText,this.dataset.tenant, this.dataset.customerid, true)">${tenantJson[i].displayName}</a></li>`; 
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
      document.getElementById("profileDropdownButton").style.backgroundImage=`url(${GenerateAvatar(profile.clientPrincipal.name.split(' ')[0].charAt(0)+profile.clientPrincipal.name.split(' ')[1].charAt(0),'#000000','#ffd017')})`;
    }
    catch
    {
      document.getElementById("profileDropdownButton").style.backgroundImage=`url(${GenerateAvatar(profile.clientPrincipal.name.charAt(0),'#000000','#ffd017')})`;
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

async function LoadUrl(url)
{
  const nextTitle = 'FFTP - NEXT';
  const nextState = { additionalInformation: 'Updated the URL with JS' };
  window.history.pushState(nextState, nextTitle, url);
  CallToAction()
}

async function CallToAction()
{

}
