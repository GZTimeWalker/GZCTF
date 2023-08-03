import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import {
  Avatar,
  Text,
  Button,
  Center,
  Grid,
  Group,
  Input,
  Modal,
  ModalProps,
  SegmentedControl,
  SimpleGrid,
  Stack,
  Textarea,
  TextInput,
  useMantineTheme,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck } from '@mdi/js'
import { Icon } from '@mdi/react'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useUser } from '@Utils/useUser'
import api, { UserInfoModel, AdminUserInfoModel, Role } from '@Api'

export const RoleColorMap = new Map<Role, string>([
  [Role.Admin, 'blue'],
  [Role.User, 'brand'],
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
  const theme = useMantineTheme()

  const [disabled, setDisabled] = useState(false)

  const [activeUser, setActiveUser] = useState<UserInfoModel>(user)
  const [profile, setProfile] = useState<AdminUserInfoModel>({})

  useEffect(() => {
    setProfile({ ...user })
    setActiveUser(user)
  }, [user])

  const onChangeProfile = () => {
    setDisabled(true)
    api.admin
      .adminUpdateUserInfo(activeUser.id!, profile)
      .then(() => {
        showNotification({
          color: 'teal',
          message: '用户信息已更新',
          icon: <Icon path={mdiCheck} size={1} />,
        })
        mutateUser({ ...activeUser, ...profile })
        modalProps.onClose()
      })
      .catch(showErrorNotification)
      .finally(() => {
        setDisabled(false)
      })
  }

  return (
    <Modal {...modalProps}>
      {/* User Info */}
      <Stack spacing="md" m="auto" mt={15}>
        <Grid grow>
          <Grid.Col span={8}>
            <TextInput
              label="用户名"
              type="text"
              w="100%"
              ff={theme.fontFamilyMonospace}
              value={profile.userName ?? 'ctfer'}
              disabled={disabled}
              onChange={(event) => setProfile({ ...profile, userName: event.target.value })}
            />
          </Grid.Col>
          <Grid.Col span={4}>
            <Center>
              <Avatar alt="avatar" radius="xl" size={70} src={activeUser.avatar}>
                {activeUser.userName?.slice(0, 1) ?? 'U'}
              </Avatar>
            </Center>
          </Grid.Col>
        </Grid>
        <Input.Wrapper label="用户角色">
          <SegmentedControl
            fullWidth
            readOnly={self?.userId === user.id}
            disabled={disabled}
            color={RoleColorMap.get(profile.role ?? Role.User)}
            value={profile.role ?? Role.User}
            onChange={(value: Role) => setProfile({ ...profile, role: value })}
            data={Object.entries(Role).map((role) => ({
              value: role[1],
              label: role[0],
            }))}
          />
        </Input.Wrapper>
        <SimpleGrid cols={2}>
          <TextInput
            label="邮箱"
            type="email"
            w="100%"
            value={profile.email ?? 'ctfer@gzti.me'}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, email: event.target.value })}
          />
          <TextInput
            label="手机号"
            type="tel"
            w="100%"
            value={profile.phone ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, phone: event.target.value })}
          />
          <TextInput
            label="学工号"
            type="number"
            w="100%"
            value={profile.stdNumber ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, stdNumber: event.target.value })}
          />
          <TextInput
            label="真实姓名"
            type="text"
            w="100%"
            value={profile.realName ?? ''}
            disabled={disabled}
            onChange={(event) => setProfile({ ...profile, realName: event.target.value })}
          />
        </SimpleGrid>
        <Textarea
          label="描述"
          value={profile.bio ?? '这个人很懒，什么都没有写'}
          w="100%"
          disabled={disabled}
          autosize
          minRows={2}
          maxRows={4}
          onChange={(event) => setProfile({ ...profile, bio: event.target.value })}
        />

        <Stack spacing={2}>
          <Group position="apart">
            <Text size="sm" fw={500}>
              用户 IP
            </Text>
            <Text size="sm" span fw={500} ff={theme.fontFamilyMonospace}>
              {user.ip}
            </Text>
          </Group>
          <Group position="apart">
            <Text size="sm" fw={500}>
              最后访问时间
            </Text>
            <Text size="sm" span fw={500} ff={theme.fontFamilyMonospace}>
              {dayjs(user.lastVisitedUTC).format('YYYY-MM-DD HH:mm:ss')}
            </Text>
          </Group>
        </Stack>

        <Group grow m="auto" w="100%">
          <Button fullWidth disabled={disabled} onClick={onChangeProfile}>
            保存信息
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

export default UserEditModal
