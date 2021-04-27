/**
 * https://stackoverflow.com/questions/58976579
 * @param {String} scriptPath
 * @returns A promise that completes when the script loads.
 */
window.loadScript = function (scriptPath) {
    if (loaded[scriptPath]) {
        return new this.Promise(function (resolve, reject) {
            resolve();
        });
    }

    return new Promise(function (resolve, reject) {
        var script = document.createElement('script');
        script.src = scriptPath;
        script.type = 'text/javascript';

        loaded[scriptPath] = true;

        script.onload = function () {
            resolve(scriptPath);
        };

        script.onerror = function () {
            reject(scriptPath);
        };

        document['body'].appendChild(script);
    });
};
loaded = [];
