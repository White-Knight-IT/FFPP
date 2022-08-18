async function FetchUrl(url, requestOptions)
{
  async function RepeatableFetch(url, requestOptions, contentType='application/json')
  {
    var signInData = await SignIn();
    requestOptions.credentials = 'include';
    requestOptions.headers= {
      'Authorization': 'Bearer '+signInData.accessToken,
      'Content-Type': contentType
    };
    let responsePayload = await fetch(url, requestOptions);
    return await responsePayload.json();
  }

  try {
      return await RepeatableFetch(url, requestOptions);
  }
  catch (e)
  {
      return await RepeatableFetch(url, requestOptions);
  }
}

async function AuthMe() {
  try
  {
    return await FetchUrl('/.auth/me', { method: 'GET' });
  }
  catch(error)
  {
    console.error(error);
  }

}

async function EditProfile(userId,tenantName,tenantDomain, tenantId, tableSize)
{
  const requestOptions = {
    method: 'PUT',
    body: JSON.stringify({ userId: userId, lastTenantName: tenantName, lastTenantDomainName: tenantDomain, lastTenantCustomerId: tenantId, defaultPageSize: tableSize})
  };
  return await FetchUrl(`/api/EditUserProfile`, requestOptions)
}

async function GetTenants(allTenantSelector = false)
{
  try
  {
    return await FetchUrl(`/api/ListTenants?AllTenantSelector=${allTenantSelector}`, { method: 'GET' });
  }
  catch(error)
  {
    console.error(error);
  }

}
