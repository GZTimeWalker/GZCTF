import { FC, useState } from 'react'
import { Divider, SimpleGrid, Stack, Switch, Title, Text } from '@mantine/core'
import AdminPage from '@Components/admin/AdminPage'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import api, { GlobalConfig } from '@Api'

const Configs: FC = () => {
  const { data: configs, mutate } = api.admin.useAdminGetConfigs({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  const [disabled, setDisabled] = useState(false)

  const updateConfig = (conf: GlobalConfig) => {
    setDisabled(true)
    api.admin
      .adminUpdateConfigs(conf)
      .then(() => {
        mutate({ ...conf })
      })
      .catch(showErrorNotification)
      .finally(() => {
        setDisabled(false)
      })
  }

  const SwitchLabel = (title: string, desrc: string) => (
    <Stack spacing={1}>
      <Text size="md" weight={500}>
        {title}
      </Text>
      <Text size="xs" color="dimmed">
        {desrc}
      </Text>
    </Stack>
  )

  return (
    <AdminPage isLoading={!configs}>
      <Stack style={{ width: '80%', minWidth: '70vw' }}>
        <Title order={2}>账户策略</Title>
        <Divider />
        <SimpleGrid cols={2}>
          <Switch
            checked={configs?.accoutPolicy?.allowRegister ?? true}
            disabled={disabled}
            label={SwitchLabel('允许新用户注册', '是否允许用户注册新账户')}
            onChange={(e) =>
              updateConfig({
                accoutPolicy: {
                  allowRegister: e.target.checked,
                  ...configs?.accoutPolicy,
                },
                ...configs,
              })
            }
          />
          <Switch
            checked={configs?.accoutPolicy?.emailConfirmationRequired ?? true}
            disabled={disabled}
            label={SwitchLabel('需要邮箱确认', '用户注册、更换邮箱、找回密码是否需要邮件确认')}
            onChange={(e) =>
              updateConfig({
                accoutPolicy: {
                  emailConfirmationRequired: e.target.checked,
                  ...configs?.accoutPolicy,
                },
                ...configs,
              })
            }
          />
          <Switch
            checked={configs?.accoutPolicy?.activeOnRegister ?? false}
            disabled={disabled}
            label={SwitchLabel('注册后自动激活', '是否在新用户注册后自动激活账户')}
            onChange={(e) =>
              updateConfig({
                accoutPolicy: {
                  activeOnRegister: e.target.checked,
                  ...configs?.accoutPolicy,
                },
                ...configs,
              })
            }
          />
          <Switch
            checked={configs?.accoutPolicy?.useGoogleRecaptcha ?? false}
            disabled={disabled}
            label={SwitchLabel('使用谷歌验证码', '是否在用户发送验证邮件时检查谷歌验证码有效性')}
            onChange={(e) =>
              updateConfig({
                accoutPolicy: {
                  useGoogleRecaptcha: e.target.checked,
                  ...configs?.accoutPolicy,
                },
                ...configs,
              })
            }
          />
        </SimpleGrid>
      </Stack>
    </AdminPage>
  )
}

export default Configs
