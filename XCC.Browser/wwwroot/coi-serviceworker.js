/* coi-serviceworker - enables cross-origin isolation on GitHub Pages
 * Based on https://github.com/gzuidhof/coi-serviceworker (MIT license)
 * Required so SharedArrayBuffer is available for SkiaSharp's WASM native bindings.
 */
if (typeof window === "undefined") {
    // Running as a service worker: intercept fetches and inject COOP/COEP headers.
    self.addEventListener("install", () => self.skipWaiting());
    self.addEventListener("activate", e => e.waitUntil(self.clients.claim()));
    self.addEventListener("fetch", function (event) {
        if (event.request.cache === "only-if-cached" && event.request.mode !== "same-origin") return;
        event.respondWith(
            fetch(event.request)
                .then(function (response) {
                    if (response.status === 0) return response;
                    const headers = new Headers(response.headers);
                    headers.set("Cross-Origin-Opener-Policy", "same-origin");
                    headers.set("Cross-Origin-Embedder-Policy", "require-corp");
                    return new Response(response.body, {
                        status: response.status,
                        statusText: response.statusText,
                        headers: headers,
                    });
                })
                .catch(err => console.error(err))
        );
    });
} else {
    // Running in the page: register this file as a service worker, then reload.
    if (!window.crossOriginIsolated && "serviceWorker" in navigator) {
        navigator.serviceWorker
            .register(document.currentScript.src)
            .then(reg => {
                function reload() {
                    if (!sessionStorage.getItem("coi-reload")) {
                        sessionStorage.setItem("coi-reload", "1");
                        location.reload();
                    }
                }
                if (reg.active && !navigator.serviceWorker.controller) {
                    reload();
                }
                reg.addEventListener("updatefound", () => {
                    reg.installing.addEventListener("statechange", function () {
                        if (this.state === "activated") reload();
                    });
                });
                navigator.serviceWorker.addEventListener("controllerchange", reload);
            })
            .catch(err => console.error("coi-serviceworker registration failed:", err));
    }
    if (window.crossOriginIsolated) {
        sessionStorage.removeItem("coi-reload");
    }
}
