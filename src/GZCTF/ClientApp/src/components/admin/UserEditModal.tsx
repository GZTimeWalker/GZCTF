import {
  Avatar,
  Button,
  Center,
  Grid,
  Group,
  Modal,
  ModalProps,
  Radio,
  SimpleGrid,
  Stack,
  Text,
  Textarea,
  TextInput,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useUser } from '@Utils/useUser'
import api, { AdminUserInfoModel, Role, UserInfoModel } from '@Api'

export const RoleColorMap = new Map<Role, string>([
  [Role.Admin, 'blue'],
  [Role.User, 'green'],
  [Role.Monitor, 'yellow'],
  [Role.Banned, 'red'],
])

interface UserEditModalProps extends ModalProps {
  user: UserInfoModel
  mutateUser: (user: UserInfoModel) => void
}

const UserEditModal: FC<UserEditModalProps> = (props) => {
  const { user, mutateUser, ...modalProps } = props
  const { user: self } = useUser()

  const [disabled, setDisabled] = useState(false)
  const [profile, setProfile] = useState<AdminUserInfoModel>({})

  const { t } = useTranslation()
  const isSelf = self?.userId === user.id

  useEffect(() => {
    setProfile({ ...user })
  }, [user])

  const onChangeProfile = () => {
    if (!user.id) return

    setDisabled(true)
    api.admin
      .adminUpdateUserInfo(user.id, profile)
      .then(() => {
        showNotification({
          color: 'teal',
          message: t('admin.notification.users.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutateUser({ ...user, ...profile })
        modalProps.onClose()
      })
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <Modal {...modalProps}>
      {/* User Info */}
      <Stack gap="md" m="auto" mt={15}>
        <Grid grow>
          <Grid.Col span={8}>
            <TextInput
              label={t('account.label.username')}
              type="text"
              w="100%"
              value={profile.userName ?? 'ctfer'}
              disabled={disabled}
              onChange={(event) => setProfile({ ...profile, userName: event.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={4}>
            <Center>
              <Avatar alt="avatar" radius="xl" size={70} src={user.avatar}>
                {user.userName?.slice(0, 1) ?? 'U'}
              </Avatar>
            </Center>
          </Grid.Col>
        </Grid>
        <Radio.Group
          label={t('admin.label.users.role')}
          value={profile.role as Role | undefined}
          onChange={(value) => {
            setProfile({ ...profile, role: value as Role })
          }}
        >
          <Group grow mt="xs">
            {Object.keys(Role).map((role) => (
              <Radio
                key={role}
                value={role}
                label={
                  <Text size="sm" fw="bold">
                    {role}
                  </Text>
                }
                color={RoleColorMap.get(role as Role)}
                disabled={disabled || isSelf}
              />
            ))}
          </Group>
        </Radio.Group>
        <SimpleGrid cols={2}>
          <TextInput
            label={t('account.label.email')}
            type="email"
            w="100%"
            value={profile.email ?? 'ctfer@gzti.me'}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, email: event.target.value })}
          />
          <TextInput
            label={t('account.label.phone')}
            type="tel"
            w="100%"
            value={profile.phone ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, phone: event.target.value })}
          />
          <TextInput
            label={t('account.label.student_id')}
            type="text"
            w="100%"
            value={profile.stdNumber ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, stdNumber: event.target.value })}
          />
          <TextInput
            label={t('account.label.real_name')}
            type="text"
            w="100%"
            value={profile.realName ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, realName: event.target.value })}
          />
        </SimpleGrid>
        <Textarea
          label={t('account.label.bio')}
          value={profile.bio ?? t('account.placeholder.bio')}
          w="100%"
          disabled={disabled}
          autosize
          minRows={2}
          maxRows={4}
          onChange={(event) => setProfile({ ...profile, bio: event.target.value })}
        />

        <Stack gap={2}>
          <Group justify="space-between">
            <Text size="sm" fw={500}>
              {t('common.label.ip')}
            </Text>
            <Text size="sm" span fw={500} ff="monospace">
              {user.ip}
            </Text>
          </Group>
          <Group justify="space-between">
            <Text size="sm" fw={500}>
              {t('admin.label.users.last_visit')}
            </Text>
            <Text size="sm" span fw={500} ff="monospace">
              {dayjs(user.lastVisitedUtc).format('YYYY-MM-DD HH:mm:ss')}
            </Text>
          </Group>
        </Stack>

        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onChangeProfile}>
            {t('admin.button.save')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default UserEditModal
