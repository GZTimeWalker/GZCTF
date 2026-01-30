import { Anchor, Checkbox, Modal, ScrollArea, Text } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { FC } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { useConfig } from '@Hooks/useConfig'

interface TermsOfServiceProps {
    checked: boolean
    onChange: (value: boolean) => void
}

export const TermsOfService: FC<TermsOfServiceProps> = ({ checked, onChange }) => {
    const { config } = useConfig()
    const { t } = useTranslation()
    const [opened, { open, close }] = useDisclosure(false)

    if (!config.enableBrowserFingerprint) return null

    return (
        <>
            <Modal
                opened={opened}
                onClose={close}
                title={t('account.tos.title')}
                centered
                scrollAreaComponent={ScrollArea.Autosize}
            >
                <Text size="sm">{t('account.tos.content')}</Text>
            </Modal>
            <Checkbox
                checked={checked}
                onChange={(event) => onChange(event.currentTarget.checked)}
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
        </>
    )
}
