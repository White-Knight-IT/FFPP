/* Don't put secret configuration settings in this file, this is rendered
by the client. */

const config = {
  auth: {
    clientId: '14eacaa6-e80b-455d-aebe-3e672f49122d',
    authority: 'https://login.microsoftonline.com/organizations/',
    redirectUri: '/index.html',
    postLogoutRedirectUri: '/bye.html'
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false
  },
  api: {
    scopes: ['https://whiteknightit.com.au/2c20f8ad-550b-49ff-a14c-aaf9dcf8c264/ffpp-api.access'],
    requiresInit: true
  }
};