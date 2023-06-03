// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

let siteConsent = null;
let telemetryInitialized = false;

function enableTelemetry() {
  if (!telemetryInitialized) {
    telemetryInitialized = true;

    // Setup dataLayer
    window.dataLayer = window.dataLayer || [];
    function gtag() {
      dataLayer.push(arguments);
    }
    gtag('js', new Date());

    // Enable Google Analytics
    window['ga-disable-UA-89203408-1'] = false;
    gtag('config', 'UA-89203408-1');

    // Enable new Google G4 analytics
    window['ga-disable-G-130J0SE94H'] = false;
    gtag('config', 'G-130J0SE94H');
  }
}

function disableTelemetry() {
  if (telemetryInitialized) {
    telemetryInitialized = false;
    window['ga-disable-G-130J0SE94H'] = true;
  }
}

function wcp_ready(err, _siteConsent) {
  if (err != undefined) {
    console.error(err);
  } else {
    siteConsent = _siteConsent;
    onConsentChanged();
  }
}

function onConsentChanged() {
  const userConsent = siteConsent.getConsentFor(WcpConsent.consentCategories.Analytics);
  if (!siteConsent.isConsentRequired || userConsent) {
    enableTelemetry();
  } else {
    disableTelemetry();
  }
}

function manageCookies() {
  siteConsent.manageConsent();
  window.scroll({
    top: 0,
    left: 0,
    behavior: 'smooth',
  });
}

$(document).ready(function () {
  WcpConsent.init('en-US', 'cookie-banner', wcp_ready, onConsentChanged);
});
