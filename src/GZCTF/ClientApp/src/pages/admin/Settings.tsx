import { Button, Divider, Grid, SimpleGrid, Stack, Switch, TextInput, Title } from '@mantine/core'
import { mdiCheck, mdiContentSaveOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import AdminPage from '@Components/admin/AdminPage'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useFixedButtonStyles } from '@Utils/ThemeOverride'
import { OnceSWRConfig, useConfig } from '@Utils/useConfig'
import api, { AccountPolicy, ConfigEditModel, ContainerPolicy, GlobalConfig } from '@Api'

const Configs: FC = () => {
  const { data: configs, mutate } = api.admin.useAdminGetConfigs(OnceSWRConfig)

  const { mutate: mutateConfig } = useConfig()
  const [disabled, setDisabled] = useState(false)
  const [globalConfig, setGlobalConfig] = useState<GlobalConfig | null>()
  const [accountPolicy, setAccountPolicy] = useState<AccountPolicy | null>()
  const [containerPolicy, setContainerPolicy] = useState<ContainerPolicy | null>()

  const { t } = useTranslation()

  const [saved, setSaved] = useState(true)
  const { classes: btnClasses } = useFixedButtonStyles({
    right: 'calc(0.05 * (100vw - 70px - 2rem) + 1rem)',
    bottom: '2rem',
  })

  useEffect(() => {
    if (configs) {
      setContainerPolicy(configs.containerPolicy)
      setGlobalConfig(configs.globalConfig)
      setAccountPolicy(configs.accountPolicy)
    }
  }, [configs])

  const updateConfig = (conf: ConfigEditModel) => {
    setDisabled(true)
    api.admin
      .adminUpdateConfigs(conf)
      .then(() => {
        mutate({ ...conf })
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        mutateConfig({ ...conf.globalConfig })
        setDisabled(false)
      })
  }

  return (
    <AdminPage isLoading={!configs}>
      <Button
        className={btnClasses.fixedButton}
        variant="filled"
        radius="xl"
        size="md"
        leftIcon={<Icon path={saved ? mdiContentSaveOutline : mdiCheck} size={1} />}
        onClick={() => {
          updateConfig({ globalConfig, accountPolicy, containerPolicy })
          setSaved(false)
          setTimeout(() => setSaved(true), 500)
        }}
        disabled={!saved}
      >
        {t('admin.button.back')}
      </Button>
      <Stack w="100%" spacing="xl">
        <Stack>
          <Title order={2}>{t('admin.content.settings.platform.title')}</Title>
          <Divider />
          <Grid columns={4}>
            <Grid.Col span={1}>
              <TextInput
                label={t('admin.content.settings.platform.name.label')}
                description={t('admin.content.settings.platform.name.description')}
                placeholder="GZ"
                value={globalConfig?.title ?? ''}
                onChange={(e) => {
                  setGlobalConfig({ ...(globalConfig ?? {}), title: e.currentTarget.value })
                }}
              />
            </Grid.Col>
            <Grid.Col span={1}>
              <TextInput
                label={t('admin.content.settings.platform.slogan.label')}
                description={t('admin.content.settings.platform.slogan.description')}
                placeholder="Hack for fun not for profit"
                value={globalConfig?.slogan ?? ''}
                onChange={(e) => {
                  setGlobalConfig({ ...(globalConfig ?? {}), slogan: e.currentTarget.value })
                }}
              />
            </Grid.Col>
            <Grid.Col span={2}>
              <TextInput
                label={t('admin.content.settings.platform.footer.label')}
                description={t('admin.content.settings.platform.footer.description')}
                placeholder={t('admin.placeholder.settings.footer')}
                value={globalConfig?.footerInfo ?? ''}
                onChange={(e) => {
                  setGlobalConfig({ ...(globalConfig ?? {}), footerInfo: e.currentTarget.value })
                }}
              />
            </Grid.Col>
          </Grid>
        </Stack>

        <Stack>
          <Title order={2}>{t('admin.content.settings.account.title')}</Title>
          <Divider />
          <SimpleGrid cols={4}>
            <Switch
              checked={accountPolicy?.allowRegister ?? true}
              disabled={disabled}
              label={SwitchLabel(
                t('admin.content.settings.account.allow_register.label'),
                t('admin.content.settings.account.allow_register.description')
              )}
              onChange={(e) =>
                setAccountPolicy({
                  ...(accountPolicy ?? {}),
                  allowRegister: e.currentTarget.checked,
                })
              }
            />
            <Switch
              checked={accountPolicy?.emailConfirmationRequired ?? false}
              disabled={disabled}
              label={SwitchLabel(
                t('admin.content.settings.account.email_confirmation_required.label'),
                t('admin.content.settings.account.email_confirmation_required.description')
              )}
              onChange={(e) =>
                setAccountPolicy({
                  ...(accountPolicy ?? {}),
                  emailConfirmationRequired: e.currentTarget.checked,
                })
              }
            />
            <Switch
              checked={accountPolicy?.activeOnRegister ?? true}
              disabled={disabled}
              label={SwitchLabel(
                t('admin.content.settings.account.auto_active.label'),
                t('admin.content.settings.account.auto_active.description')
              )}
              onChange={(e) =>
                setAccountPolicy({
                  ...(accountPolicy ?? {}),
                  activeOnRegister: e.currentTarget.checked,
                })
              }
            />
            <Switch
              checked={accountPolicy?.useCaptcha ?? false}
              disabled={disabled}
              label={SwitchLabel(
                t('admin.content.settings.account.use_captcha.label'),
                t('admin.content.settings.account.use_captcha.description')
              )}
              onChange={(e) =>
                setAccountPolicy({
                  ...(accountPolicy ?? {}),
                  useCaptcha: e.currentTarget.checked,
                })
              }
            />
          </SimpleGrid>
          <TextInput
            label={t('admin.content.settings.account.email_domain_list.label')}
            description={t('admin.content.settings.account.email_domain_list.description')}
            placeholder={t('admin.placeholder.settings.email_domain_list')}
            value={accountPolicy?.emailDomainList ?? ''}
            onChange={(e) => {
              setAccountPolicy({ ...(accountPolicy ?? {}), emailDomainList: e.currentTarget.value })
            }}
          />
        </Stack>

        <Stack>
          <Title order={2}>{t('admin.content.settings.game.title')}</Title>
          <Divider />
          <SimpleGrid cols={2}>
            <Switch
              checked={containerPolicy?.autoDestroyOnLimitReached ?? true}
              disabled={disabled}
              label={SwitchLabel(
                t('admin.content.settings.game.auto_destroy.label'),
                t('admin.content.settings.game.auto_destroy.description')
              )}
              onChange={(e) =>
                setContainerPolicy({
                  ...(containerPolicy ?? {}),
                  autoDestroyOnLimitReached: e.currentTarget.checked,
                })
              }
            />
          </SimpleGrid>
        </Stack>
      </Stack>
    </AdminPage>
  )
}

export default Configs
