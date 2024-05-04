import {
  ActionIcon,
  Avatar,
  Badge,
  Button,
  Code,
  Group,
  Paper,
  ScrollArea,
  Stack,
  Switch,
  Table,
  Text,
  TextInput,
} from '@mantine/core'
import { useClipboard, useInputState } from '@mantine/hooks'
import { useModals } from '@mantine/modals'
import { showNotification } from '@mantine/notifications'
import {
  mdiAccountOutline,
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiCheck,
  mdiDeleteOutline,
  mdiLockReset,
  mdiMagnify,
  mdiPencilOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import React, { FC, useEffect, useRef, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import AdminPage from '@Components/admin/AdminPage'
import UserEditModal, { RoleColorMap } from '@Components/admin/UserEditModal'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useTableStyles } from '@Utils/ThemeOverride'
import { useArrayResponse } from '@Utils/useArrayResponse'
import { useUser } from '@Utils/useUser'
import api, { Role, UserInfoModel } from '@Api'

const ITEM_COUNT_PER_PAGE = 30

const Users: FC = () => {
  const [page, setPage] = useState(1)
  const [update, setUpdate] = useState(new Date())
  const [editModalOpened, setEditModalOpened] = useState(false)
  const [activeUser, setActiveUser] = useState<UserInfoModel>({})
  const {
    data: users,
    total,
    setData: setUsers,
    updateData: updateUsers,
  } = useArrayResponse<UserInfoModel>()
  const [hint, setHint] = useInputState('')
  const [searching, setSearching] = useState(false)
  const [disabled, setDisabled] = useState(false)
  const [current, setCurrent] = useState(0)

  const modals = useModals()
  const { user: currentUser } = useUser()
  const clipboard = useClipboard()
  const { classes } = useTableStyles()

  const { t } = useTranslation()
  const viewport = useRef<HTMLDivElement>(null)

  useEffect(() => {
    viewport.current?.scrollTo({ top: 0, behavior: 'smooth' })
  }, [page, viewport])

  useEffect(() => {
    api.admin
      .adminUsers({
        count: ITEM_COUNT_PER_PAGE,
        skip: (page - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((res) => {
        setUsers(res.data)
        setCurrent((page - 1) * ITEM_COUNT_PER_PAGE + res.data.length)
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
          setCurrent((page - 1) * ITEM_COUNT_PER_PAGE + res.data.length)
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
        setCurrent(res.data.length)
      })
      .catch((e) => showErrorNotification(e, t))
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
          updateUsers(
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
      .catch((e) => showErrorNotification(e, t))
      .finally(() => {
        setDisabled(false)
      })
  }

  const onResetPassword = async (user: UserInfoModel) => {
    setDisabled(true)
    try {
      const res = await api.admin.adminResetPassword(user.id!)

      modals.openModal({
        title: t('admin.content.users.reset.title', {
          name: user.userName,
        }),

        children: (
          <Stack>
            <Text>
              <Trans i18nKey="admin.content.users.reset.content" />
            </Text>
            <Text fw="bold" ff="monospace">
              {res.data}
            </Text>
            <Button
              onClick={() => {
                clipboard.copy(res.data)
                showNotification({
                  message: t('admin.notification.users.password_copied'),
                  color: 'teal',
                  icon: <Icon path={mdiCheck} size={1} />,
                })
              }}
            >
              {t('common.button.copy')}
            </Button>
          </Stack>
        ),
      })
    } catch (err: any) {
      showErrorNotification(err, t)
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
        message: t('admin.notification.users.deleted', {
          name: user.userName,
        }),
        color: 'teal',
        icon: <Icon path={mdiCheck} size={1} />,
      })
      users && updateUsers(users.filter((x) => x.id !== user.id))
      setCurrent(current - 1)
      setUpdate(new Date())
    } catch (e: any) {
      showErrorNotification(e, t)
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
            w="30%"
            leftSection={<Icon path={mdiMagnify} size={1} />}
            placeholder={t('admin.placeholder.users.search')}
            value={hint}
            onChange={setHint}
            onKeyDown={(e) => {
              !searching && e.key === 'Enter' && onSearch()
            }}
            rightSection={<Icon path={mdiAccountOutline} size={1} />}
          />
          <Group justify="right">
            <Text fw="bold" size="sm">
              <Trans
                i18nKey="admin.content.users.stats"
                values={{
                  current,
                  total,
                }}
              >
                _<Code>_</Code>_
              </Trans>
            </Text>
            <ActionIcon size="lg" disabled={page <= 1} onClick={() => setPage(page - 1)}>
              <Icon path={mdiArrowLeftBold} size={1} />
            </ActionIcon>
            <Text fw="bold" size="sm">
              {page}
            </Text>
            <ActionIcon
              size="lg"
              disabled={page * ITEM_COUNT_PER_PAGE >= total}
              onClick={() => setPage(page + 1)}
            >
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </>
      }
    >
      <Paper shadow="md" p="xs" w="100%">
        <ScrollArea
          viewportRef={viewport}
          offsetScrollbars
          scrollbarSize={4}
          h="calc(100vh - 190px)"
        >
          <Table className={classes.table}>
            <Table.Thead>
              <Table.Tr>
                <Table.Th style={{ minWidth: '1.8rem' }}>{t('admin.label.users.active')}</Table.Th>
                <Table.Th>{t('common.label.user')}</Table.Th>
                <Table.Th>{t('account.label.email')}</Table.Th>
                <Table.Th>{t('common.label.ip')}</Table.Th>
                <Table.Th>{t('account.label.real_name')}</Table.Th>
                <Table.Th>{t('account.label.student_id')}</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {users &&
                users.map((user) => (
                  <Table.Tr key={user.id}>
                    <Table.Td>
                      <Switch
                        disabled={disabled}
                        checked={user.emailConfirmed ?? false}
                        onChange={() => onToggleActive(user)}
                      />
                    </Table.Td>
                    <Table.Td>
                      <Group wrap="nowrap" justify="space-between" gap="xs">
                        <Group wrap="nowrap" justify="left">
                          <Avatar alt="avatar" src={user.avatar} radius="xl">
                            {user.userName?.slice(0, 1) ?? 'U'}
                          </Avatar>
                          <Text ff="monospace" size="sm" fw="bold" lineClamp={1}>
                            {user.userName}
                          </Text>
                        </Group>
                        <Badge size="sm" color={RoleColorMap.get(user.role ?? Role.User)}>
                          {user.role}
                        </Badge>
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" ff="monospace" lineClamp={1}>
                        {user.email}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text lineClamp={1} size="sm" ff="monospace">
                        {user.ip}
                      </Text>
                    </Table.Td>
                    <Table.Td>{user.realName ?? t('admin.placeholder.users.real_name')}</Table.Td>
                    <Table.Td>
                      <Text size="sm" ff="monospace">
                        {user.stdNumber ?? t('admin.placeholder.users.student_id')}
                      </Text>
                    </Table.Td>
                    <Table.Td align="right">
                      <Group wrap="nowrap" gap="sm" justify="right">
                        <ActionIcon
                          color="blue"
                          onClick={() => {
                            setActiveUser(user)
                            setEditModalOpened(true)
                          }}
                        >
                          <Icon path={mdiPencilOutline} size={1} />
                        </ActionIcon>
                        <ActionIconWithConfirm
                          iconPath={mdiLockReset}
                          color="orange"
                          message={t('admin.content.users.reset.message', {
                            name: user.userName,
                          })}
                          disabled={disabled}
                          onClick={() => onResetPassword(user)}
                        />
                        <ActionIconWithConfirm
                          iconPath={mdiDeleteOutline}
                          color="alert"
                          message={t('admin.content.users.delete', {
                            name: user.userName,
                          })}
                          disabled={disabled || user.id === currentUser?.userId}
                          onClick={() => onDelete(user)}
                        />
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
            </Table.Tbody>
          </Table>
        </ScrollArea>
        <UserEditModal
          size="35%"
          title={t('admin.button.users.edit')}
          user={activeUser}
          opened={editModalOpened}
          onClose={() => setEditModalOpened(false)}
          mutateUser={(user: UserInfoModel) => {
            updateUsers(
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
