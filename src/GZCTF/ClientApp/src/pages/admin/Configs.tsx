import { FC, useEffect, useState } from 'react'
import { Button, Divider, Grid, SimpleGrid, Stack, Switch, TextInput, Title } from '@mantine/core'
import { mdiCheck, mdiContentSaveOutline } from '@mdi/js'
import { Icon } from '@mdi/react'
import AdminPage from '@Components/admin/AdminPage'
import { SwitchLabel } from '@Components/admin/SwitchLabel'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useFixedButtonStyles } from '@Utils/ThemeOverride'
import { OnceSWRConfig, useConfig } from '@Utils/useConfig'
import api, { AccountPolicy, ConfigEditModel, GamePolicy, GlobalConfig } from '@Api'

const Configs: FC = () => {
  const { data: configs, mutate } = api.admin.useAdminGetConfigs(OnceSWRConfig)

  const { mutate: mutateConfig } = useConfig()
  const [disabled, setDisabled] = useState(false)
  const [globalConfig, setGlobalConfig] = useState<GlobalConfig | null>()
  const [accountPolicy, setAccountPolicy] = useState<AccountPolicy | null>()
  const [gamePolicy, setGamePolicy] = useState<GamePolicy | null>()

  const [saved, setSaved] = useState(true)
  const { classes: btnClasses } = useFixedButtonStyles({
    right: 'calc(0.05 * (100vw - 70px - 2rem) + 1rem)',
    bottom: '2rem',
  })

  useEffect(() => {
    if (configs) {
      setGamePolicy(configs.gamePolicy)
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
      .catch(showErrorNotification)
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
          updateConfig({ globalConfig, accountPolicy, gamePolicy })
          setSaved(false)
          setTimeout(() => setSaved(true), 500)
        }}
        disabled={!saved}
      >
        保存配置
      </Button>
      <Stack w="100%" spacing="xl">
        <Stack>
          <Title order={2}>平台设置</Title>
          <Divider />
          <Grid columns={4}>
            <Grid.Col span={1}>
              <TextInput
                label="平台名称"
                description="平台名称显示后跟 ::CTF 字段"
                placeholder="GZ"
                value={globalConfig?.title ?? ''}
                onChange={(e) => {
                  setGlobalConfig({ ...(globalConfig ?? {}), title: e.currentTarget.value })
                }}
              />
            </Grid.Col>
            <Grid.Col span={1}>
              <TextInput
                label="平台标语"
                description="平台标语将显示在页面顶部和关于页面"
                placeholder="Hack for fun not for profit"
                value={globalConfig?.slogan ?? ''}
                onChange={(e) => {
                  setGlobalConfig({ ...(globalConfig ?? {}), slogan: e.currentTarget.value })
                }}
              />
            </Grid.Col>
            <Grid.Col span={2}>
              <TextInput
                label="页脚附加信息"
                description="显示在网页底部的附加信息"
                placeholder="某 IPC 备 XXXXXXXX 号-X 某公网安备 XXXXXXXXXXXXXX 号"
                value={globalConfig?.footerInfo ?? ''}
                onChange={(e) => {
                  setGlobalConfig({ ...(globalConfig ?? {}), footerInfo: e.currentTarget.value })
                }}
              />
            </Grid.Col>
          </Grid>
        </Stack>

        <Stack>
          <Title order={2}>账户策略</Title>
          <Divider />
          <SimpleGrid cols={4}>
            <Switch
              checked={accountPolicy?.allowRegister ?? true}
              disabled={disabled}
              label={SwitchLabel('允许新用户注册', '是否允许用户注册新账户')}
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
              label={SwitchLabel('需要邮箱确认', '用户是否需要邮件确认身份')}
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
              label={SwitchLabel('注册后自动激活', '是否在新用户注册后自动激活账户')}
              onChange={(e) =>
                setAccountPolicy({
                  ...(accountPolicy ?? {}),
                  activeOnRegister: e.currentTarget.checked,
                })
              }
            />
            <Switch
              checked={accountPolicy?.useGoogleRecaptcha ?? false}
              disabled={disabled}
              label={SwitchLabel('使用谷歌验证码', '是否在发送验证邮件时进行验证码校验')}
              onChange={(e) =>
                setAccountPolicy({
                  ...(accountPolicy ?? {}),
                  useGoogleRecaptcha: e.currentTarget.checked,
                })
              }
            />
          </SimpleGrid>
          <TextInput
            label="可用邮箱域名列表"
            description="允许注册的邮箱域名列表，多个域名用逗号分隔，留空则不限制"
            placeholder="不限制注册域名"
            value={accountPolicy?.emailDomainList ?? ''}
            onChange={(e) => {
              setAccountPolicy({ ...(accountPolicy ?? {}), emailDomainList: e.currentTarget.value })
            }}
          />
        </Stack>

        <Stack>
          <Title order={2}>比赛策略</Title>
          <Divider />
          <SimpleGrid cols={2}>
            <Switch
              checked={gamePolicy?.autoDestroyOnLimitReached ?? true}
              disabled={disabled}
              label={SwitchLabel(
                '自动销毁旧实例',
                '是否在用户开启题目实例但达到上限时自动销毁旧实例'
              )}
              onChange={(e) =>
                setGamePolicy({
                  ...(gamePolicy ?? {}),
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
