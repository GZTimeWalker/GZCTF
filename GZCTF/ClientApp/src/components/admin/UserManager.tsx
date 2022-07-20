import { FC, useEffect, useState } from 'react'
import {
  Group,
  Stack,
  Table,
  Text,
  ActionIcon,
  Badge,
  Avatar,
  Paper,
  NumberInput,
  useMantineTheme,
} from '@mantine/core'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import {
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiCheck,
  mdiClose,
  mdiDeleteOutline,
  mdiFileEditOutline,
} from '@mdi/js'
import Icon from '@mdi/react'
import api, { Role, UserInfoModel } from '../../Api'
import UserEditModal from './edit/UserEditModal'

const ITEM_COUNT_PER_PAGE = 30

const RoleColorMap = new Map<Role, string>([
  [Role.Admin, 'blue'],
  [Role.User, 'brand'],
  [Role.Monitor, 'yellow'],
  [Role.BannedUser, 'red'],
])

const UserManager: FC = () => {
  const [page, setPage] = useState(1)
  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [activeUser, setActiveUser] = useState<UserInfoModel>({})
  const [users, setUsers] = useState<UserInfoModel[]>([])

  const theme = useMantineTheme()
  const { data: currentUser } = api.account.useAccountProfile({
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

  useEffect(() => {
    api.admin
      .adminUsers({ count: ITEM_COUNT_PER_PAGE, skip: (page - 1) * ITEM_COUNT_PER_PAGE })
      .then((res) => {
        setUsers(res.data)
      })
  }, [page])

  const modals = useModals()

  const onConfirmDelete = (user: UserInfoModel) => {
    api.admin
      .adminDeleteUser(user.id!)
      .then(() => {
        showNotification({
          color: 'teal',
          message: '用户已删除',
          icon: <Icon path={mdiCheck} size={1} />,
          disallowClose: true,
        })
        setUsers(users?.filter((t) => t.id !== user.id) ?? [])
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
  }

  const onDeleteUser = (user: UserInfoModel) => {
    if (!user) {
      return
    }

    if (user.id === currentUser?.userId) {
      showNotification({
        color: 'orange',
        message: '不可以删除自己',
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    modals.openConfirmModal({
      title: '删除用户',
      children: <Text size="sm">你确定要删除用户 "{user.userName}" 吗？</Text>,
      onConfirm: () => onConfirmDelete(user),
      centered: true,
      labels: { confirm: '删除用户', cancel: '取消' },
      confirmProps: { color: 'red' },
    })
  }

  return (
    <Paper shadow="md" p="md">
      <Stack>
        <Group position="apart">
          <Badge size="xl" radius="sm" variant="outline">
            第 {page} 页
          </Badge>
          <Group position="right">
            <ActionIcon
              size="lg"
              variant="hover"
              disabled={page <= 1}
              onClick={() => setPage(page - 1)}
            >
              <Icon path={mdiArrowLeftBold} size={1} />
            </ActionIcon>
            <ActionIcon
              size="lg"
              variant="hover"
              disabled={users && users.length < ITEM_COUNT_PER_PAGE}
              onClick={() => setPage(page + 1)}
            >
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </Group>
        <Table>
          <thead>
            <tr>
              <th>用户</th>
              <th>邮箱</th>
              <th>最后一次访问</th>
              <th>真实姓名</th>
              <th>学号</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {users &&
              users.map((user) => (
                <tr key={user.id}>
                  <td>
                    <Group position="apart">
                      <Group position="left">
                        <Avatar src={user.avatar} radius="xl" />
                        <Text>{user.userName}</Text>
                      </Group>
                      <Badge size="md" color={RoleColorMap.get(user.role ?? Role.User)}>
                        {user.role}
                      </Badge>
                    </Group>
                  </td>
                  <td>
                    <Text size="sm" style={{ fontFamily: theme.fontFamilyMonospace }}>
                      {user.email}
                    </Text>
                  </td>
                  <td>
                    <Group position="apart">
                      <Text size="sm" style={{ fontFamily: theme.fontFamilyMonospace }}>
                        {user.ip}
                      </Text>
                      <Badge size="md" color="cyan" variant="outline">
                        {new Date(user.lastVisitedUTC!).toLocaleString()}
                      </Badge>
                    </Group>
                  </td>
                  <td>{!user.realName ? '用户未填写' : user.realName}</td>
                  <td>
                    <Text size="sm" style={{ fontFamily: theme.fontFamilyMonospace }}>
                      {!user.stdNumber ? '00000000' : user.stdNumber}
                    </Text>
                  </td>
                  <td>
                    <Group>
                      <ActionIcon
                        onClick={() => {
                          console.log(user)
                          setActiveUser(user)
                          setIsEditModalOpen(true)
                        }}
                      >
                        <Icon path={mdiFileEditOutline} size={1} />
                      </ActionIcon>
                      <ActionIcon
                        disabled={user.id === currentUser?.userId}
                        onClick={() => onDeleteUser(user)}
                        color="red"
                      >
                        <Icon path={mdiDeleteOutline} size={1} />
                      </ActionIcon>
                    </Group>
                  </td>
                </tr>
              ))}
          </tbody>
        </Table>
        <UserEditModal
          centered
          size="30%"
          title="编辑用户"
          user={activeUser}
          opened={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          mutateUser={(user: UserInfoModel) => {
            setUsers(
              [user, ...(users?.filter((n) => n.id !== user.id) ?? [])].sort((a, b) =>
                a.id! < b.id! ? -1 : 1
              )
            )
          }}
        />
      </Stack>
    </Paper>
  )
}

export default UserManager
