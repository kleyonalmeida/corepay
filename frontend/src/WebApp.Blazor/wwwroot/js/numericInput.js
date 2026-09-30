export function registerInputGuard(element, kind) {
    if (!element) {
        return { dispose: () => { } };
    }

    const beforeInputHandler = (event) => {
        if (event.inputType === "insertFromPaste") {
            return;
        }

        if (event.data === null || event.data === undefined) {
            return;
        }

        if (kind === "Percent") {
            if (/^\d$/.test(event.data)) {
                return;
            }

            if (event.data === "," || event.data === ".") {
                if (element.value.includes(",")) {
                    event.preventDefault();
                }

                return;
            }

            event.preventDefault();
            return;
        }

        if (!/^\d$/.test(event.data)) {
            event.preventDefault();
        }
    };

    element.addEventListener("beforeinput", beforeInputHandler);

    return {
        dispose: () => element.removeEventListener("beforeinput", beforeInputHandler)
    };
}
