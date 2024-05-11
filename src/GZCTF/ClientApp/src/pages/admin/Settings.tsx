import { generateColors } from '@mantine/colors-generator'
import {
  Button,
  ColorInput,
  Divider,
  Grid,
  InputBase,
  NumberInput,
  SimpleGrid,
  Stack,
  Switch,
  TextInput,
  Title,
  useMantineTheme,
} from '@mantine/core'
import { mdiCheck, mdiContentSaveOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import ColorPreview from '@Components/ColorPreview'
import AdminPage from '@Components/admin/AdminPage'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import { showErrorNotification } from '@Utils/ApiHelper'
import { OnceSWRConfig, useConfig } from '@Utils/useConfig'
import api, { AccountPolicy, ConfigEditModel, ContainerPolicy, GlobalConfig } from '@Api'
import btnClasses from '@Styles/FixedButton.module.css'

const Configs: FC = () => {
  const { data: configs, mutate } = api.admin.useAdminGetConfigs(OnceSWRConfig)

  const { mutate: mutateConfig } = useConfig()
  const [disabled, setDisabled] = useState(false)
  const [globalConfig, setGlobalConfig] = useState<GlobalConfig | null>()
  const [accountPolicy, setAccountPolicy] = useState<AccountPolicy | null>()
  const [containerPolicy, setContainerPolicy] = useState<ContainerPolicy | null>()
  const [color, setColor] = useState<string | undefined | null>(globalConfig?.customTheme)

  const { t } = useTranslation()

  const [saved, setSaved] = useState(true)
  const theme = useMantineTheme()

  useEffect(() => {
    if (configs) {
      setContainerPolicy(configs.containerPolicy)
      setGlobalConfig(configs.globalConfig)
      setAccountPolicy(configs.accountPolicy)
      setColor(configs.globalConfig?.customTheme)
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
        mutateConfig({ ...conf.globalConfig, ...conf.containerPolicy })
        setDisabled(false)
      })
  }

  const colors = color && /^#[0-9A-F]{6}$/i.test(color) ? generateColors(color) : theme.colors.brand

  return (
    <AdminPage isLoading={!configs}>
      <Button
        className={btnClasses.root}
        __vars={{
          '--fixed-right': 'calc(0.05 * (100vw - 70px - 2rem) + 1rem)',
        }}
        variant="filled"
        radius="xl"
        size="md"
        leftSection={<Icon path={saved ? mdiContentSaveOutline : mdiCheck} size={1} />}
        onClick={() => {
          updateConfig({
            globalConfig: {
              ...(globalConfig ?? {}),
              customTheme: color && /^#[0-9A-F]{6}$/i.test(color) ? color : '',
            },
            accountPolicy,
            containerPolicy,
          })
          setSaved(false)
          setTimeout(() => {
            setSaved(true)
          }, 500)
        }}
        disabled={!saved}
      >
        {t('admin.button.save')}
      </Button>
      <Stack w="100%" gap="xl">
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
            <Grid.Col span={1}>
              <ColorInput
                label={t('admin.content.settings.platform.color.label')}
                description={t('admin.content.settings.platform.color.description')}
                placeholder={t('common.content.color.custom.placeholder')}
                value={color ?? ''}
                onChange={setColor}
              />
            </Grid.Col>
            <Grid.Col span={3}>
              <InputBase
                label={t('admin.content.settings.platform.color_palette.label')}
                description={t('admin.content.settings.platform.color_palette.description')}
                variant="unstyled"
                component={ColorPreview}
                h="100%"
                colors={colors}
                displayColorsInfo={false}
                styles={{
                  input: {
                    display: 'flex',
                  },
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
          <Title order={2}>{t('admin.content.settings.container.title')}</Title>
          <Divider />
          <SimpleGrid cols={4} style={{ alignItems: 'center' }}>
            <NumberInput
              label={t('admin.content.settings.container.default_lifetime.label')}
              description={t('admin.content.settings.container.default_lifetime.description')}
              placeholder="120"
              min={1}
              max={7200}
              disabled={disabled}
              value={containerPolicy?.defaultLifetime ?? 120}
              onChange={(e) => {
                if (typeof e === 'string') return

                const num = e ? Math.min(Math.max(e, 1), 7200) : 120
                setContainerPolicy({ ...(containerPolicy ?? {}), defaultLifetime: num })
              }}
            />
            <NumberInput
              label={t('admin.content.settings.container.extension_duration.label')}
              description={t('admin.content.settings.container.extension_duration.description')}
              placeholder="120"
              min={1}
              max={7200}
              disabled={disabled}
              value={containerPolicy?.extensionDuration ?? 120}
              onChange={(e) => {
                if (typeof e === 'string') return

                const num = e ? Math.min(Math.max(e, 1), 7200) : 120
                setContainerPolicy({ ...(containerPolicy ?? {}), extensionDuration: num })
              }}
            />
            <NumberInput
              label={t('admin.content.settings.container.renewal_window.label')}
              description={t('admin.content.settings.container.renewal_window.description')}
              placeholder="10"
              min={1}
              max={360}
              disabled={disabled}
              value={containerPolicy?.renewalWindow ?? 10}
              onChange={(e) => {
                if (typeof e === 'string') return

                const num = e ? Math.min(Math.max(e, 1), 360) : 10
                setContainerPolicy({ ...(containerPolicy ?? {}), renewalWindow: num })
              }}
            />
            <Switch
              checked={containerPolicy?.autoDestroyOnLimitReached ?? true}
              disabled={disabled}
              label={SwitchLabel(
                t('admin.content.settings.container.auto_destroy.label'),
                t('admin.content.settings.container.auto_destroy.description')
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
