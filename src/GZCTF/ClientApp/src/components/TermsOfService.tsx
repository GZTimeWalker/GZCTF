import { Anchor, Button, Checkbox, Group, Modal, ScrollArea, Text } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { FC } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { useConfig } from '@Hooks/useConfig'

interface TermsOfServiceProps {
    checked?: boolean
    onChange?: (value: boolean) => void
    opened?: boolean
    onClose?: () => void
    onAccept?: () => void
    confirmMode?: boolean
}

export const TermsOfService: FC<TermsOfServiceProps> = ({
    checked = false,
    onChange,
    opened: controlledOpened,
    onClose,
    onAccept,
    confirmMode = false,
}) => {
    const { config } = useConfig()
    const { t } = useTranslation()
    const [previewOpened, { open, close }] = useDisclosure(false)
    const noop = () => {}
    const modalOpened = confirmMode ? Boolean(controlledOpened) : previewOpened
    const closeModal = confirmMode ? (onClose ?? noop) : close

    if (!config.enableBrowserFingerprint) return null

    return (
        <>
            <Modal
                opened={modalOpened}
                onClose={closeModal}
                title={t('account.tos.title')}
                centered
                scrollAreaComponent={ScrollArea.Autosize}
            >
                <Text size="sm">{t('account.tos.content')}</Text>
                {confirmMode && (
                    <Group justify="flex-end" mt="md">
                        <Button variant="default" onClick={onClose ?? noop}>
                            {t('common.modal.cancel')}
                        </Button>
                        <Button onClick={onAccept ?? noop}>{t('account.button.accept_tos')}</Button>
                    </Group>
                )}
            </Modal>
            {!confirmMode && (
                <Checkbox
                    checked={checked}
                    onChange={(event) => onChange?.(event.currentTarget.checked)}
                    label={
                        <Trans i18nKey="account.label.accept_tos">
                            I agree to the
                            <Anchor
                                component="button"
                                type="button"
                                onClick={(e: React.MouseEvent) => {
                                    e.preventDefault()
                                    open()
                                }}
                                fz="sm"
                            >
                                Terms of Service
                            </Anchor>
                        </Trans>
                    }
                    mb="sm"
                />
            )}
        </>
    )
}
