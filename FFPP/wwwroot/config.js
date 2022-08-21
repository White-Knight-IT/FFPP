/* Don't put secret configuration settings in this file, this is rendered
by the client. */

const config = {
  auth: {
    clientId: 'ff5c79ea-29dc-4ec0-b49c-2421bb8c5c27',
    authority: 'https://login.microsoftonline.com/organizations/',
    redirectUri: '/index.html',
    postLogoutRedirectUri: '/bye.html'
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false
  },
  api: {
    scopes: ['https://whiteknightit.com.au/1692aa88-9600-49c2-98db-95c5a4993bad/ffpp-api.access'],
    requiresInit: true
  }
};