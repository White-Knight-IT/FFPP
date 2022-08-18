var homeAccountIdGlobal;
var tokenGlobal;

async function SignIn() {

  async function FetchToken(account, myMsal)
  {
    var tokenRequest = {
      scopes: config.api.scopes,
      account: account
    }

    let tokenResponse = await myMsal.acquireTokenSilent(tokenRequest);
    //console.log("Token Response: ", tokenResponse);

    return tokenResponse;
  }

  var loginRequest = {
    scopes: config.api.scopes
  };

  let accountId = "";
  let account ="";

  const myMsal = new msal.PublicClientApplication(config);

  async function handleResponse(response) {
    let accessToken ="";

    if (response !== null) {
      accountId = response.account.homeAccountId;
      account = response.account;
      // Display signed-in user content, call API, etc.
      //console.log("Response not null so use it to fetch token");
      var token = await FetchToken(response.account, myMsal);
      accessToken = token.accessToken;
    }
    else
    {
      // In case multiple accounts exist, you can select
      const currentAccounts = myMsal.getAllAccounts();

      if (currentAccounts.length === 0)
      {
        // no accounts signed-in, attempt to sign a user in
        console.log("No accounts, attempt to sign in user");
        await myMsal.loginRedirect(loginRequest);
        console.log("User should have signed in");
        var token = await FetchToken(response.account, myMsal);
        accessToken = token.accessToken;
      }
      else if (currentAccounts.length > 1)
      {
        // Add choose account code here
        var token = await FetchToken(response.account, myMsal);
        accessToken = token.accessToken;
        console.log("Multiple accounts");
      }
      else if (currentAccounts.length === 1)
      {
        //console.log("Single account");
        accountId = currentAccounts[0].homeAccountId;
        account = currentAccounts[0];
        var token = await FetchToken(account, myMsal);
        accessToken = token.accessToken;
      }
      var signInData = {account:account, accessToken:accessToken};
      homeAccountIdGlobal = accountId;
      tokenGlobal = signInData.accessToken;
      return signInData;
    }
  }
  return myMsal.handleRedirectPromise().then(handleResponse);
}

async function SignOut()
{ 
  const myMsal = new msal.PublicClientApplication(config);
  
  const logoutRequest = {
    account: myMsal.getAccountByHomeId(homeAccountIdGlobal),
  };
  
  myMsal.logoutRedirect(logoutRequest);
}
