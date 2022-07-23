import React, { FC, useEffect, useState } from 'react'
import {
  useMantineTheme,
  TextInput,
  Group,
  ActionIcon,
  Paper,
  Table,
  Avatar,
  Text,
  AvatarsGroup,
} from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import {
  mdiClose,
  mdiMagnify,
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiLockOutline,
  mdiDeleteOutline,
  mdiPencilOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import api, { TeamInfoModel } from '../../Api'
import AdminPage from '../../components/admin/AdminPage'

const ITEM_COUNT_PER_PAGE = 30

const Teams: FC = () => {
  const [page, setPage] = useState(1)
  const [teams, setTeams] = useState<TeamInfoModel[]>([])
  const [hint, setHint] = useInputState('')
  const [searching, setSearching] = useState(false)

  const theme = useMantineTheme()

  useEffect(() => {
    api.admin
      .adminTeams({
        count: ITEM_COUNT_PER_PAGE,
        skip: (page - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((res) => {
        setTeams(res.data)
      })
  }, [page])

  const onSearch = () => {
    if (!hint) {
      api.admin
        .adminTeams({
          count: ITEM_COUNT_PER_PAGE,
          skip: (page - 1) * ITEM_COUNT_PER_PAGE,
        })
        .then((res) => {
          setTeams(res.data)
        })
      return
    }

    setSearching(true)

    api.admin
      .adminSearchTeams({
        hint,
      })
      .then((res) => {
        setTeams(res.data)
      })
      .catch((err) => {
        showNotification({
          color: 'red',
          title: '遇到了问题',
          message: `${err.error.title}`,
          icon: <Icon path={mdiClose} size={1} />,
        })
      })
      .finally(() => {
        setSearching(false)
      })
  }

  return (
    <AdminPage
      head={
        <>
          <TextInput
            icon={<Icon path={mdiMagnify} size={1} />}
            style={{ width: '30%' }}
            placeholder="搜索队伍名称"
            value={hint}
            onChange={setHint}
            onKeyDown={(e) => {
              !searching && e.key === 'Enter' && onSearch()
            }}
          />
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
              disabled={teams && teams.length < ITEM_COUNT_PER_PAGE}
              onClick={() => setPage(page + 1)}
            >
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </>
      }
    >
      <Paper shadow="md" p="md">
        <Table>
          <thead>
            <tr>
              <th>队伍</th>
              <th>签名</th>
              <th>队员</th>
              <th>操作</th>
            </tr>
          </thead>
          <tbody>
            {teams &&
              teams.map((team) => {
                const members = team.members && [
                  team.members.filter((m) => m.captain).at(0)!,
                  ...(team.members.filter((m) => !m.captain) ?? []),
                ]

                return (
                  <tr key={team.id}>
                    <td>
                      <Group position="apart">
                        <Group position="left">
                          <Avatar src={team.avatar} radius="xl">
                            {team.name?.at(0)}
                          </Avatar>
                          <Text>{team.name}</Text>
                        </Group>
                        {team.locked && (
                          <Icon path={mdiLockOutline} size={1} color={theme.colors.orange[1]} />
                        )}
                      </Group>
                    </td>
                    <td>
                      <Text lineClamp={1} style={{ overflow: 'hidden' }}>
                        {team.bio}
                      </Text>
                    </td>
                    <td>
                      <AvatarsGroup
                        limit={6}
                        size="md"
                        styles={{
                          child: {
                            border: 'none',
                          },
                        }}
                      >
                        {members && members.map((m) => <Avatar key={m.id} src={m.avatar} />)}
                      </AvatarsGroup>
                    </td>
                    <td>
                      <Group>
                        <ActionIcon>
                          <Icon path={mdiPencilOutline} size={1} />
                        </ActionIcon>
                        <ActionIcon color="red">
                          <Icon path={mdiDeleteOutline} size={1} />
                        </ActionIcon>
                      </Group>
                    </td>
                  </tr>
                )
              })}
          </tbody>
        </Table>
      </Paper>
    </AdminPage>
  )
}

export default Teams
