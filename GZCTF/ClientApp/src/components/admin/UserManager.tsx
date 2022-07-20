import { FC, useState } from 'react'
import { Group, Stack, Table, Title, Text, ActionIcon, Badge } from '@mantine/core'
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
import api, { UserInfoModel } from '../../Api'
import UserEditModal from './edit/UserEditModal'

const ITEM_COUNT_PER_PAGE = 30

const UserManager: FC = () => {
  const [activePage, setPage] = useState(-1)
  const [disabled, setDisabled] = useState(false)
  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [activeUser, setActiveUser] = useState<UserInfoModel>({})

  const { data: users, mutate } = api.admin.useAdminUsers(undefined, {
    refreshInterval: 0,
    revalidateIfStale: false,
    revalidateOnFocus: false,
  })

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
        mutate(users?.filter((t) => t.id !== user.id) ?? [])
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
    if (disabled && !user) {
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
    <Stack>
      <Group position="apart">
        <Badge size="xl" radius="sm" variant="outline">
          第 {activePage} 页
        </Badge>
        <Group position="right">
          <ActionIcon
            size="lg"
            variant="hover"
            disabled={activePage <= 1}
            onClick={() => setPage(activePage - 1)}
          >
            <Icon path={mdiArrowLeftBold} size={1} />
          </ActionIcon>
          <ActionIcon
            size="lg"
            variant="hover"
            disabled={users ? users.length < ITEM_COUNT_PER_PAGE : false}
            onClick={() => setPage(activePage + 1)}
          >
            <Icon path={mdiArrowRightBold} size={1} />
          </ActionIcon>
        </Group>
      </Group>
      <Table>
        <thead>
          <tr>
            <th>用户名</th>
            <th>用户角色</th>
            <th>所拥有的队伍</th>
            <th>激活的队伍</th>
            <th>操作</th>
          </tr>
        </thead>
        <tbody>
          {users?.map((user) => (
            <tr key={user.id}>
              <td>{user.userName}</td>
              <td>{user.role}</td>
              <td>{user.ownTeamName}</td>
              <td>{user.activeTeamName}</td>
              <td>
                <Group>
                  <ActionIcon
                    disabled={disabled}
                    onClick={() => {
                      setActiveUser(user)
                      setIsEditModalOpen(true)
                    }}
                  >
                    <Icon path={mdiFileEditOutline} size={1} />
                  </ActionIcon>
                  <ActionIcon disabled={disabled} onClick={() => onDeleteUser(user)} color="red">
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
        basicInfo={activeUser}
        opened={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        mutateUser={(user: UserInfoModel) => {
          mutate([user, ...(users?.filter((n) => n.id !== user.id) ?? [])])
        }}
      />
    </Stack>
  )
}

export default UserManager
