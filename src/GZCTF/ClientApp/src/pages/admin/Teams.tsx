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
  Code,
  Badge,
  Input,
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
  mdiPencilOutline,
  mdiLockOpenVariantOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import AdminPage from '@Components/admin/AdminPage'
import TeamEditModal from '@Components/admin/TeamEditModal'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useTableStyles, useTooltipStyles } from '@Utils/ThemeOverride'
import { useArrayResponse } from '@Utils/useArrayResponse'
import api, { TeamInfoModel, TeamWithDetailedUserInfo } from '@Api'

const ITEM_COUNT_PER_PAGE = 30

const Teams: FC = () => {
  const [page, setPage] = useState(1)
  const [update, setUpdate] = useState(new Date())
  const {
    data: teams,
    total,
    setData: setTeams,
    updateData: updateTeams,
  } = useArrayResponse<TeamInfoModel>()
  const [hint, setHint] = useInputState('')
  const [searching, setSearching] = useState(false)
  const [disabled, setDisabled] = useState(false)
  const [current, setCurrent] = useState(0)
  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [activeTeam, setActiveTeam] = useState<TeamWithDetailedUserInfo>({})

  const { classes } = useTableStyles()
  const { classes: tooltipClasses } = useTooltipStyles()

  useEffect(() => {
    api.admin
      .adminTeams({
        count: ITEM_COUNT_PER_PAGE,
        skip: (page - 1) * ITEM_COUNT_PER_PAGE,
      })
      .then((res) => {
        setTeams(res.data)
        setCurrent((page - 1) * ITEM_COUNT_PER_PAGE + res.data.length)
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
          setCurrent((page - 1) * ITEM_COUNT_PER_PAGE + res.data.length)
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
        setCurrent((page - 1) * ITEM_COUNT_PER_PAGE + res.data.length)
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
      })
      teams && updateTeams(teams.filter((x) => x.id !== team.id))
      setCurrent(current - 1)
      setUpdate(new Date())
    } catch (e: any) {
      showErrorNotification(e)
    } finally {
      setDisabled(false)
    }
  }

  const onToggleLock = async (team: TeamInfoModel) => {
    try {
      if (!team.id) return
      setDisabled(true)

      await api.admin.adminUpdateTeam(team.id!, {
        locked: !team.locked,
      })

      showNotification({
        color: 'teal',
        message: '队伍信息已更新',
        icon: <Icon path={mdiCheck} size={1} />,
      })

      updateTeams(
        [{ ...team, locked: !team.locked }, ...(teams?.filter((n) => n.id !== team.id) ?? [])].sort(
          (a, b) => (a.id! < b.id! ? -1 : 1)
        )
      )
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
            w="30%"
            icon={<Icon path={mdiMagnify} size={1} />}
            placeholder="搜索队伍名称"
            value={hint}
            onChange={setHint}
            onKeyDown={(e) => {
              !searching && e.key === 'Enter' && onSearch()
            }}
          />
          <Group position="right">
            <Text fw="bold" size="sm">
              已显示 <Code>{current}</Code> / <Code>{total}</Code> 队伍
            </Text>
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
      <Paper shadow="md" p="md" w="100%">
        <ScrollArea offsetScrollbars scrollbarSize={4} h="calc(100vh - 190px)">
          <Table className={classes.table}>
            <thead>
              <tr>
                <th style={{ width: '23rem' }}>队伍</th>
                <th>队员</th>
                <th>签名</th>
                <th />
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
                        <Group position="apart" spacing={0}>
                          <Group position="left">
                            <Avatar alt="avatar" src={team.avatar} radius="xl">
                              {team.name?.slice(0, 1)}
                            </Avatar>
                            <Input
                              variant="unstyled"
                              value={team.name ?? 'team'}
                              readOnly
                              sx={() => ({
                                input: {
                                  userSelect: 'none',
                                  fontWeight: 'bold',
                                  width: '14rem',
                                },
                              })}
                            />
                          </Group>

                          <Badge size="sm" color={team.locked ? 'yellow' : 'gray'}>
                            {team.locked ? '已锁定' : '未锁定'}
                          </Badge>
                        </Group>
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
                                  classNames={tooltipClasses}
                                >
                                  <Avatar alt="avatar" radius="xl" src={m.avatar}>
                                    {m.userName?.slice(0, 1) ?? 'U'}
                                  </Avatar>
                                </Tooltip>
                              ))}
                            {members && members.length > 8 && (
                              <Tooltip label={<Text>{members.slice(8).join(',')}</Text>} withArrow>
                                <Avatar alt="avatar" radius="xl">
                                  +{members.length - 8}
                                </Avatar>
                              </Tooltip>
                            )}
                          </Avatar.Group>
                        </Tooltip.Group>
                      </td>
                      <td>
                        <Text lineClamp={1} style={{ overflow: 'hidden' }}>
                          {team.bio ?? '这个队伍很懒，什么都没有写'}
                        </Text>
                      </td>
                      <td align="right">
                        <Group noWrap spacing="sm" position="right">
                          <ActionIcon
                            color="blue"
                            onClick={() => {
                              setActiveTeam(team)
                              setIsEditModalOpen(true)
                            }}
                          >
                            <Icon path={mdiPencilOutline} size={1} />
                          </ActionIcon>

                          <ActionIconWithConfirm
                            iconPath={team.locked ? mdiLockOpenVariantOutline : mdiLockOutline}
                            color={team.locked ? 'gray' : 'yellow'}
                            message={`确定要${team.locked ? '解锁' : '锁定'}队伍\n“${
                              team.name
                            }” 吗？`}
                            disabled={disabled}
                            onClick={() => onToggleLock(team)}
                          />

                          <ActionIconWithConfirm
                            iconPath={mdiDeleteOutline}
                            color="alert"
                            message={`确定要删除队伍\n“${team.name}” 吗？`}
                            disabled={disabled}
                            onClick={() => onDelete(team)}
                          />
                        </Group>
                      </td>
                    </tr>
                  )
                })}
            </tbody>
          </Table>
        </ScrollArea>
        <TeamEditModal
          size="35%"
          title="编辑队伍"
          team={activeTeam}
          opened={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          mutateTeam={(team: TeamWithDetailedUserInfo) => {
            updateTeams(
              [team, ...(teams?.filter((n) => n.id !== team.id) ?? [])].sort((a, b) =>
                a.id! < b.id! ? -1 : 1
              )
            )
          }}
        />
      </Paper>
    </AdminPage>
  )
}

export default Teams
