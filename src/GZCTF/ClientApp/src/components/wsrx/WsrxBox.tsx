import { ActionIcon, Group, Stack, Text, TextInput } from "@mantine/core";
import { showNotification } from "@mantine/notifications";
import { mdiRefresh, mdiTuneVertical } from "@mdi/js";
import Icon from "@mdi/react";
import { WsrxError, WsrxErrorKind, WsrxState } from "@xdsec/wsrx";
import { FC, useEffect, useState } from "react";
import { Trans, useTranslation } from "react-i18next";
import { useConfig } from "@Hooks/useConfig";
import { useWsrx } from "@Hooks/useWsrx";

/**
 * WsrxBox component
 * Wsrx's state will be managed by this component.
 *
 */
export const WsrxBox: FC<{}> = () => {
	const { wsrx, wsrxOptions, setWsrxOptions } = useWsrx();
	const platformConfig = useConfig();
	const [wsrxState, setWsrxState] = useState(wsrx.getState());
	const { t } = useTranslation();

	wsrx.onStateChange((state) => {
		setWsrxState(state);
	});

	const [showConfig, setShowConfig] = useState(false);

	useEffect(() => {
		if (platformConfig) {
			setWsrxOptions({
				...wsrxOptions,
				name: (platformConfig.config.title || "GZ") + "::CTF",
			});
			wsrx.connect().catch(() => {});
		}
	}, [platformConfig.config.title]);

	function retryConnect() {
		wsrx.connect().catch((err) => {
			if (err instanceof WsrxError) {
				switch (err.kind) {
					case WsrxErrorKind.VersionMismatch:
						showNotification({
							color: "orange",
							title: t("wsrx.errors.version_mismatch"),
							message: t("wsrx.errors.version_mismatch_msg"),
							autoClose: 5000,
						});
						break;
					case WsrxErrorKind.DaemonUnavailable:
						showNotification({
							color: "red",
							title: t("wsrx.errors.daemon_unavailable"),
							message: t("wsrx.errors.daemon_unavailable_msg"),
							autoClose: 5000,
						});
						break;
					case WsrxErrorKind.DaemonError:
						showNotification({
							color: "red",
							title: t("wsrx.errors.daemon_error"),
							message: t("wsrx.errors.daemon_error_msg"),
							autoClose: 5000,
						});
						break;
					default:
						showNotification({
							color: "red",
							title: t("wsrx.errors.unknown_error"),
							message: t("wsrx.errors.unknown_error_msg"),
							autoClose: 5000,
						});
				}
			}
		});
	}

	return (
		<Stack gap="xs">
			<Group h="30px" wrap="nowrap" justify="space-between" gap={2}>
				{wsrxState === WsrxState.Usable && (
					<Text fw="bold" c="green" flex={1}>
						<Trans i18nKey="wsrx.state.usable" />
					</Text>
				)}
				{wsrxState === WsrxState.Pending && (
					<Text fw="bold" c="orange" flex={1}>
						<Trans i18nKey="wsrx.state.pending" />
					</Text>
				)}
				{wsrxState === WsrxState.Invalid && (
					<Text fw="bold" c="orange" flex={1}>
						<Trans i18nKey="wsrx.state.invalid" />
					</Text>
				)}
				<ActionIcon
					variant="subtle"
					aria-label={t("wsrx.action.retry")}
					onClick={retryConnect}
					loading={wsrxState === WsrxState.Pending}
				>
					<Icon path={mdiRefresh} size={1} />
				</ActionIcon>
				<ActionIcon
					variant="subtle"
					aria-label={t("wsrx.action.toggle_config")}
					onClick={() => setShowConfig(!showConfig)}
				>
					<Icon path={mdiTuneVertical} size={1} />
				</ActionIcon>
			</Group>
			{showConfig && (
				<>
					<Group h="30px" wrap="nowrap" justify="space-between" gap={2}>
						<TextInput
							size="xs"
							flex={1}
							placeholder="127.0.0.1:3307"
							value={wsrxOptions.api}
							onChange={(e) =>
								setWsrxOptions({ ...wsrxOptions, api: e.currentTarget.value })
							}
						/>
					</Group>
				</>
			)}
		</Stack>
	);
};
