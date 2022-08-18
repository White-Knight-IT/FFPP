/* Don't put secret configuration settings in this file, this is rendered
by the client. */

const config = {
  auth: {
    clientId: 'a76159c9-150b-4272-948c-622d2f71880e',
    authority: 'https://login.microsoftonline.com/organizations/',
    redirectUri: '/index.html',
    postLogoutRedirectUri: '/bye.html'
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false
  },
  api: {
    scopes: ['https://whiteknightit.onmicrosoft.com/f51a7304-a700-48af-b739-dae545d11ec6/ffpp-api.access'],
    requiresInit: true
  }
};