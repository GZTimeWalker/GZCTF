import { FC, useEffect, useState } from 'react'
import { Group, Stack, Table, Text, ActionIcon, Badge, Paper, NumberInput } from '@mantine/core'
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

const UserManager: FC = () => {
  const [count, setCount] = useState(20);
  const [skip, setSkip] = useState(0);
  const [disabled, setDisabled] = useState(false)
  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [activeUser, setActiveUser] = useState<UserInfoModel>({})
  const [users, setUsers] = useState<UserInfoModel[]>([])

  useEffect(() => {
    api.admin.adminUsers({ count, skip })
      .then((res) => {
        setUsers(res.data);
      })
  }, [count, skip])

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
    <Paper shadow="md" p="md">
      <Stack>
        <Group position="apart">
          <Group position="left">
            <NumberInput label="条目数量" required value={count} min={1} precision={0} onChange={(val) => setCount(val!)} />
            <NumberInput label="偏移" required value={skip} min={0} precision={0} onChange={(val) => setSkip(val!)} />
          </Group>
          <Group position="right">
            <ActionIcon
              size="lg"
              variant="hover"
              disabled={skip < count}
              onClick={() => setSkip(skip - count)}
            >
              <Icon path={mdiArrowLeftBold} size={1} />
            </ActionIcon>
            <ActionIcon
              size="lg"
              variant="hover"
              disabled={users ? users.length < count : false}
              onClick={() => setSkip(skip + count)}
            >
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </Group>
        <Table>
          <thead>
            <tr>
              <th>用户名</th>
              <th>邮箱</th>
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
                <td>{user.email}</td>
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
            setUsers([user, ...(users?.filter((n) => n.id !== user.id) ?? [])])
          }}
        />
      </Stack>
    </Paper>
  )
}

export default UserManager
