// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

var siteConsent = null;
var telemetryInitialized = false;

function enableTelemetry() {
    if (!telemetryInitialized) {
        telemetryInitialized = true;

        // setup dataLayer
        window.dataLayer = window.dataLayer || [];
        function gtag(){dataLayer.push(arguments);}
        gtag('js', new Date());

        // enable google analytics.
        window['ga-disable-UA-89203408-1'] = false;
        gtag('config', 'UA-89203408-1');

        // enable new google G4 analytics.
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

function wcp_ready(err, _siteConsent){
    if (err != undefined) {
        return error;
    } else {
        siteConsent = _siteConsent;
        onConsentChanged();
    }
}

function onConsentChanged() {
    var userConsent = siteConsent.getConsentFor(WcpConsent.consentCategories.Analytics);
    if (!siteConsent.isConsentRequired) {
        enableTelemetry();
    }
    else if (userConsent) {
        enableTelemetry();
    }
    else {
        disableTelemetry()
    }
}

function manageCookies() {
    siteConsent.manageConsent();
    window.scroll({
        top: 0,
        left: 0,
        behavior: 'smooth'
      });
}

$(document).ready(function () {
    WcpConsent.init("en-US", "cookie-banner", wcp_ready, onConsentChanged);
});
