/* Don't put secret configuration settings in this file, this is rendered
by the client. */

const config = {
  auth: {
    clientId: '3f8e8f00-dbc7-4994-b9c4-78e31e8603f5',
    authority: 'https://login.microsoftonline.com/organizations/',
    redirectUri: '/index.html',
    postLogoutRedirectUri: '/bye.html'
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false
  },
  api: {
    scopes: ['https://whiteknightit.com.au/abb7b956-f01a-4f4b-bd3b-94b2e35969ba/ffpp-api.access'],
    requiresInit: true
  }
};