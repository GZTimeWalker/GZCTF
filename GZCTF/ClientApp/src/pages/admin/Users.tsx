import React, { FC, useEffect, useState } from 'react'
import {
  Group,
  Table,
  Text,
  ActionIcon,
  Badge,
  Avatar,
  TextInput,
  Paper,
  ScrollArea,
  Switch,
  Stack,
  Button,
} from '@mantine/core'
import { useClipboard, useInputState } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import {
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiCheck,
  mdiDeleteOutline,
  mdiLockReset,
  mdiMagnify,
  mdiPencilOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import AdminPage from '@Components/admin/AdminPage'
import UserEditModal, { RoleColorMap } from '@Components/admin/UserEditModal'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useTableStyles } from '@Utils/ThemeOverride'
import { useUser } from '@Utils/useUser'
import api, { Role, UserInfoModel } from '@Api'

const ITEM_COUNT_PER_PAGE = 30

const Users: FC = () => {
  const [page, setPage] = useState(1)
  const [update, setUpdate] = useState(new Date())
  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [activeUser, setActiveUser] = useState<UserInfoModel>({})
  const [users, setUsers] = useState<UserInfoModel[]>()
  const [hint, setHint] = useInputState('')
  const [searching, setSearching] = useState(false)
  const [disabled, setDisabled] = useState(false)

  const modals = useModals()
  const { user: currentUser } = useUser()
  const clipboard = useClipboard()
  const { classes, theme } = useTableStyles()

  useEffect(() => {
    api.admin
      .adminUsers({
        count: ITEM_COUNT_PER_PAGE,
        skip: (page - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((res) => {
        setUsers(res.data)
      })
  }, [page, update])

  const onSearch = () => {
    if (!hint) {
      api.admin
        .adminUsers({
          count: ITEM_COUNT_PER_PAGE,
          skip: (page - 1) * ITEM_COUNT_PER_PAGE,
        })
        .then((res) => {
          setUsers(res.data)
        })
      return
    }

    setSearching(true)

    api.admin
      .adminSearchUsers({
        hint,
      })
      .then((res) => {
        setUsers(res.data)
      })
      .catch(showErrorNotification)
      .finally(() => {
        setSearching(false)
      })
  }

  const onToggleActive = (user: UserInfoModel) => {
    setDisabled(true)
    api.admin
      .adminUpdateUserInfo(user.id!, {
        emailConfirmed: !user.emailConfirmed,
      })
      .then(() => {
        users &&
          setUsers(
            users.map((u) =>
              u.id === user.id
                ? {
                    ...u,
                    emailConfirmed: !u.emailConfirmed,
                  }
                : u
            )
          )
      })
      .catch(showErrorNotification)
      .finally(() => {
        setDisabled(false)
      })
  }

  const onResetPassword = async (user: UserInfoModel) => {
    setDisabled(true)
    try {
      const res = await api.admin.adminResetPassword(user.id!)

      modals.openModal({
        title: `为 ${user.userName} 重置密码`,
        centered: true,
        withCloseButton: false,
        children: (
          <Stack>
            <Text>
              用户密码已重置，
              <Text span weight={700}>
                此密码只会显示一次
              </Text>
              。
            </Text>
            <Text
              weight={700}
              align="center"
              sx={(theme) => ({ fontFamily: theme.fontFamilyMonospace })}
            >
              {res.data}
            </Text>
            <Button
              onClick={() => {
                clipboard.copy(res.data)
                showNotification({
                  message: '密码已复制到剪贴板',
                  color: 'teal',
                  icon: <Icon path={mdiCheck} size={1} />,
                  disallowClose: true,
                })
              }}
            >
              复制到剪贴板
            </Button>
          </Stack>
        ),
      })
    } catch (err: any) {
      showErrorNotification(err)
    } finally {
      setDisabled(false)
    }
  }

  const onDelete = async (user: UserInfoModel) => {
    try {
      setDisabled(true)
      if (!user.id) return

      await api.admin.adminDeleteUser(user.id)
      showNotification({
        message: `${user.userName} 已删除`,
        color: 'teal',
        icon: <Icon path={mdiCheck} size={1} />,
        disallowClose: true,
      })
      setUsers(users?.filter((x) => x.id !== user.id))
      setUpdate(new Date())
    } catch (e: any) {
      showErrorNotification(e)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <AdminPage
      isLoading={searching || !users}
      head={
        <>
          <TextInput
            icon={<Icon path={mdiMagnify} size={1} />}
            style={{ width: '30%' }}
            placeholder="搜索用户名/邮箱/学号/姓名"
            value={hint}
            onChange={setHint}
            onKeyDown={(e) => {
              !searching && e.key === 'Enter' && onSearch()
            }}
          />
          <Group position="right">
            <ActionIcon size="lg" disabled={page <= 1} onClick={() => setPage(page - 1)}>
              <Icon path={mdiArrowLeftBold} size={1} />
            </ActionIcon>
            <ActionIcon
              size="lg"
              disabled={users && users.length < ITEM_COUNT_PER_PAGE}
              onClick={() => setPage(page + 1)}
            >
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </>
      }
    >
      <Paper shadow="md" p="xs" style={{ width: '100%' }}>
        <ScrollArea offsetScrollbars scrollbarSize={4} style={{ height: 'calc(100vh - 190px)' }}>
          <Table className={classes.table}>
            <thead>
              <tr>
                <th>激活</th>
                <th>用户</th>
                <th>邮箱</th>
                <th>用户 IP</th>
                <th>真实姓名</th>
                <th>学号</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {users &&
                users.map((user) => (
                  <tr key={user.id}>
                    <td>
                      <Switch
                        disabled={disabled}
                        checked={user.emailConfirmed ?? false}
                        onChange={() => onToggleActive(user)}
                      />
                    </td>
                    <td>
                      <Group position="apart">
                        <Group position="left">
                          <Avatar src={user.avatar} radius="xl" />
                          <Text weight={500}>{user.userName}</Text>
                        </Group>
                        <Badge size="sm" color={RoleColorMap.get(user.role ?? Role.User)}>
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
                      <Group noWrap position="apart">
                        <Text
                          lineClamp={1}
                          size="sm"
                          style={{ fontFamily: theme.fontFamilyMonospace }}
                        >
                          {user.ip}
                        </Text>
                      </Group>
                    </td>
                    <td>{!user.realName ? '用户未填写' : user.realName}</td>
                    <td>
                      <Text size="sm" style={{ fontFamily: theme.fontFamilyMonospace }}>
                        {!user.stdNumber ? '00000000' : user.stdNumber}
                      </Text>
                    </td>
                    <td align="right">
                      <Group noWrap spacing="sm" position="right">
                        <ActionIcon
                          color="blue"
                          onClick={() => {
                            setActiveUser(user)
                            setIsEditModalOpen(true)
                          }}
                        >
                          <Icon path={mdiPencilOutline} size={1} />
                        </ActionIcon>
                        <ActionIconWithConfirm
                          iconPath={mdiLockReset}
                          color="orange"
                          message={`确定要重置 “${user.userName}” 的密码吗？`}
                          disabled={disabled}
                          onClick={() => onResetPassword(user)}
                        />
                        <ActionIconWithConfirm
                          iconPath={mdiDeleteOutline}
                          color="alert"
                          message={`确定要删除 “${user.userName}” 吗？`}
                          disabled={disabled || user.id == currentUser?.userId}
                          onClick={() => onDelete(user)}
                        />
                      </Group>
                    </td>
                  </tr>
                ))}
            </tbody>
          </Table>
        </ScrollArea>
        <UserEditModal
          centered
          size="35%"
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
      </Paper>
    </AdminPage>
  )
}

export default Users
