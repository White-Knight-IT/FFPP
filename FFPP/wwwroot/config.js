/* Don't put secret configuration settings in this file, this is rendered
by the client. */

const config = {
  auth: {
    clientId: '1aeb2b5e-cf75-45b8-b9f8-534bdd70ee3e',
    authority: 'https://login.microsoftonline.com/organizations/',
    redirectUri: '/index.html',
    postLogoutRedirectUri: '/bye.html'
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false
  },
  api: {
    scopes: ['https://whiteknightit.com.au/5f719b46-003c-40ed-939b-ee734afbfde4/ffpp-api.access'],
    requiresInit: true
  }
};