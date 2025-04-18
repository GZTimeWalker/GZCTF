import { useLocalStorage } from "@mantine/hooks";
import { showNotification } from "@mantine/notifications";
import { Wsrx, WsrxFeature, WsrxOptions, WsrxState } from "@xdsec/wsrx";
import { t } from "i18next";
import { useEffect } from "react";

const defaultWsrxOptions: WsrxOptions = {
	api: "http://127.0.0.1:3307",
	name: "GZCTF",
	features: [WsrxFeature.Basic, WsrxFeature.Pingfall],
};

const wsrx = new Wsrx(defaultWsrxOptions);
let cachedState: WsrxState | null = null;

wsrx.onStateChange((state) => {
	if (state === WsrxState.Invalid && cachedState !== WsrxState.Invalid) {
		showNotification({
			color: "red",
			title: t("wsrx.errors.daemon_offline"),
			message: t("wsrx.errors.daemon_offline_msg"),
			autoClose: 5000,
		});
	}
	cachedState = state;
});

export const useWsrx = () => {
	const [wsrxOptions, setWsrxOptions] = useLocalStorage<WsrxOptions>({
		key: "wsrx-options",
		defaultValue: defaultWsrxOptions,
	});

	useEffect(() => {
		wsrx.setOptions(wsrxOptions);
	}, [wsrxOptions]);

	return {
		wsrxOptions,
		setWsrxOptions,
		wsrx,
	};
};
