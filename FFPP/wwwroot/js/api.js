async function FetchUrl(url, requestOptions, signIn=true)
{
  async function RepeatableFetch(url, requestOptions, contentType='application/json')
  {
    if(signIn)
    {
      var signInData = await SignIn();
      requestOptions.credentials = 'include';
      requestOptions.headers= {
        'Authorization': 'Bearer '+signInData.accessToken,
        'Content-Type': contentType
      };
    }
    else
    {
      requestOptions.headers= {
        'Content-Type': contentType
      };
    }
    let responsePayload = await fetch(url, requestOptions);
    return await responsePayload.json();
  }

  try {
      return await RepeatableFetch(url, requestOptions);
  }
  catch (error)
  {
    console.error(`Error with url ${url} - ${error} - Retrying`)
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

async function TokenStatus()
{
  try
  {
    return await FetchUrl(`/bootstrap/TokenStatus`, { method: 'GET' }, false);
  }
  catch(error)
  {
    console.error(error);
  }

}

async function GraphTokenUrl()
{
  try
  {
    return await FetchUrl(`/bootstrap/GetGraphTokenUrl`, { method: 'GET' }, false);
  }
  catch(error)
  {
    console.error(error);
  }
}
