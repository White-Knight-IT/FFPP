/* Don't put secret configuration settings in this file, this is rendered
by the client. */

const config = {
  auth: {
    clientId: '6fea2e45-939b-4eb6-bd0e-ef3506dd92a0',
    authority: 'https://login.microsoftonline.com/organizations/',
    redirectUri: '/index.html',
    postLogoutRedirectUri: '/bye.html'
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false
  },
  api: {
    scopes: ['https://whiteknightit.com.au/ddde6ce0-4a8e-43da-827f-f4bf8bcaea70/ffpp-api.access'],
    requiresInit: true
  }
};