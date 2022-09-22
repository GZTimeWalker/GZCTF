import React, { FC, useEffect, useState } from 'react'
import {
  TextInput,
  Group,
  ActionIcon,
  Paper,
  Table,
  Avatar,
  Text,
  Tooltip,
  ScrollArea,
} from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import {
  mdiMagnify,
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiLockOutline,
  mdiDeleteOutline,
  mdiCheck,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import AdminPage from '@Components/admin/AdminPage'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useTableStyles, useTooltipStyles } from '@Utils/ThemeOverride'
import api, { TeamInfoModel } from '@Api'

const ITEM_COUNT_PER_PAGE = 30

const Teams: FC = () => {
  const [page, setPage] = useState(1)
  const [update, setUpdate] = useState(new Date())
  const [teams, setTeams] = useState<TeamInfoModel[]>()
  const [hint, setHint] = useInputState('')
  const [searching, setSearching] = useState(false)
  const [disabled, setDisabled] = useState(false)

  const { classes, theme } = useTableStyles()
  const { classes: tooltipClasses } = useTooltipStyles()

  useEffect(() => {
    api.admin
      .adminTeams({
        count: ITEM_COUNT_PER_PAGE,
        skip: (page - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((res) => {
        setTeams(res.data)
      })
  }, [page, update])

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
      .catch(showErrorNotification)
      .finally(() => {
        setSearching(false)
      })
  }

  const onDelete = async (team: TeamInfoModel) => {
    try {
      if (!team.id) return
      setDisabled(true)

      await api.admin.adminDeleteTeam(team.id)
      showNotification({
        message: `${team.name} 已删除`,
        color: 'teal',
        icon: <Icon path={mdiCheck} size={1} />,
        disallowClose: true,
      })
      setTeams(teams?.filter((x) => x.id !== team.id))
      setUpdate(new Date())
    } catch (e: any) {
      showErrorNotification(e)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <AdminPage
      isLoading={searching || !teams}
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
            <ActionIcon size="lg" disabled={page <= 1} onClick={() => setPage(page - 1)}>
              <Icon path={mdiArrowLeftBold} size={1} />
            </ActionIcon>
            <ActionIcon
              size="lg"
              disabled={teams && teams.length < ITEM_COUNT_PER_PAGE}
              onClick={() => setPage(page + 1)}
            >
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </>
      }
    >
      <Paper shadow="md" p="md" style={{ width: '100%' }}>
        <ScrollArea offsetScrollbars scrollbarSize={4} style={{ height: 'calc(100vh - 190px)' }}>
          <Table className={classes.table}>
            <thead>
              <tr>
                <th>队伍</th>
                <th>签名</th>
                <th>队员</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {teams &&
                teams.map((team) => {
                  const members = team.members && [
                    team.members.filter((m) => m.captain)[0]!,
                    ...(team.members.filter((m) => !m.captain) ?? []),
                  ]

                  return (
                    <tr key={team.id}>
                      <td>
                        <Group position="apart">
                          <Group position="left">
                            <Avatar src={team.avatar} radius="xl">
                              {team.name?.slice(0, 1)}
                            </Avatar>
                            <Text lineClamp={1}>{team.name}</Text>
                          </Group>
                          {team.locked && (
                            <Icon path={mdiLockOutline} size={1} color={theme.colors.yellow[6]} />
                          )}
                        </Group>
                      </td>
                      <td>
                        <Text lineClamp={1} style={{ overflow: 'hidden' }}>
                          {team.bio}
                        </Text>
                      </td>
                      <td>
                        <Tooltip.Group openDelay={300} closeDelay={100}>
                          <Avatar.Group
                            spacing="md"
                            styles={{
                              child: {
                                border: 'none',
                              },
                            }}
                          >
                            {members &&
                              members.slice(0, 8).map((m) => (
                                <Tooltip
                                  key={m.id}
                                  label={m.userName}
                                  withArrow
                                  classNames={{
                                    tooltip: tooltipClasses.tooltip,
                                    arrow: tooltipClasses.arrow,
                                  }}
                                >
                                  <Avatar radius="xl" src={m.avatar} />
                                </Tooltip>
                              ))}
                            {members && members.length > 8 && (
                              <Tooltip label={<Text>{members.slice(8).join(',')}</Text>} withArrow>
                                <Avatar radius="xl">+{members.length - 8}</Avatar>
                              </Tooltip>
                            )}
                          </Avatar.Group>
                        </Tooltip.Group>
                      </td>
                      <td align="right">
                        <ActionIconWithConfirm
                          iconPath={mdiDeleteOutline}
                          color="alert"
                          message={`确定要删除 “${team.name}” 吗？`}
                          disabled={disabled}
                          onClick={() => onDelete(team)}
                        />
                      </td>
                    </tr>
                  )
                })}
            </tbody>
          </Table>
        </ScrollArea>
      </Paper>
    </AdminPage>
  )
}

export default Teams
