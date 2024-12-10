import {
  ActionIcon,
  Avatar,
  Badge,
  Code,
  Group,
  Input,
  Paper,
  ScrollArea,
  Table,
  Text,
  TextInput,
  Tooltip,
} from '@mantine/core'
import { useInputState } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import {
  mdiAccountGroupOutline,
  mdiArrowLeftBold,
  mdiArrowRightBold,
  mdiCheck,
  mdiDeleteOutline,
  mdiLockOpenVariantOutline,
  mdiLockOutline,
  mdiMagnify,
  mdiPencilOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import cx from 'clsx'
import React, { FC, useEffect, useRef, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import { AdminPage } from '@Components/admin/AdminPage'
import { TeamEditModal } from '@Components/admin/TeamEditModal'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useArrayResponse } from '@Hooks/useArrayResponse'
import api, { TeamInfoModel, TeamWithDetailedUserInfo } from '@Api'
import misc from '@Styles/Misc.module.css'
import tableClasses from '@Styles/Table.module.css'
import tooltipClasses from '@Styles/Tooltip.module.css'

const ITEM_COUNT_PER_PAGE = 30

const Teams: FC = () => {
  const [page, setPage] = useState(1)
  const [update, setUpdate] = useState(new Date())
  const { data: teams, total, setData: setTeams, updateData: updateTeams } = useArrayResponse<TeamInfoModel>()
  const [hint, setHint] = useInputState('')
  const [searching, setSearching] = useState(false)
  const [disabled, setDisabled] = useState(false)
  const [current, setCurrent] = useState(0)
  const [isEditModalOpen, setIsEditModalOpen] = useState(false)
  const [activeTeam, setActiveTeam] = useState<TeamWithDetailedUserInfo>({})

  const { t } = useTranslation()
  const viewport = useRef<HTMLDivElement>(null)

  useEffect(() => {
    viewport.current?.scrollTo({ top: 0, behavior: 'smooth' })
  }, [page, viewport])

  useEffect(() => {
    const fetchData = async () => {
      try {
        const res = await api.admin.adminTeams({
          count: ITEM_COUNT_PER_PAGE,
          skip: (page - 1) * ITEM_COUNT_PER_PAGE,
        })

        setTeams(res.data)
        setCurrent((page - 1) * ITEM_COUNT_PER_PAGE + res.data.length)
      } catch (e) {
        showErrorNotification(e, t)
      }
    }

    fetchData()
  }, [page, update])

  const onSearch = async () => {
    try {
      if (!hint) {
        const res = await api.admin.adminTeams({
          count: ITEM_COUNT_PER_PAGE,
          skip: (page - 1) * ITEM_COUNT_PER_PAGE,
        })

        setTeams(res.data)
        setCurrent((page - 1) * ITEM_COUNT_PER_PAGE + res.data.length)
      } else {
        setSearching(true)

        const res = await api.admin.adminSearchTeams({ hint })
        setTeams(res.data)
        setCurrent(res.data.length)
      }
    } catch (e) {
      showErrorNotification(e, t)
    } finally {
      setSearching(false)
    }
  }

  const onDelete = async (team: TeamInfoModel) => {
    try {
      if (!team.id) return
      setDisabled(true)

      await api.admin.adminDeleteTeam(team.id)

      showNotification({
        message: t('admin.notification.teams.deleted', {
          name: team.name,
        }),
        color: 'teal',
        icon: <Icon path={mdiCheck} size={1} />,
      })
      if (teams) updateTeams(teams.filter((x) => x.id !== team.id))
      setCurrent(current - 1)
      setUpdate(new Date())
    } catch (e: any) {
      showErrorNotification(e, t)
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
        message: t('team.notification.updated'),
        icon: <Icon path={mdiCheck} size={1} />,
      })

      updateTeams(
        [{ ...team, locked: !team.locked }, ...(teams?.filter((n) => n.id !== team.id) ?? [])].sort((a, b) =>
          a.id! < b.id! ? -1 : 1
        )
      )
      setUpdate(new Date())
    } catch (e: any) {
      showErrorNotification(e, t)
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
            w="36%"
            leftSection={<Icon path={mdiMagnify} size={1} />}
            placeholder={t('admin.placeholder.teams.search')}
            value={hint}
            onChange={setHint}
            onKeyDown={(e) => {
              if (!searching && e.key === 'Enter') onSearch()
            }}
            rightSection={<Icon path={mdiAccountGroupOutline} size={1} />}
          />
          <Group justify="right">
            <Text fw="bold" size="sm">
              <Trans
                i18nKey="admin.content.teams.stats"
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
            <ActionIcon size="lg" disabled={page * ITEM_COUNT_PER_PAGE >= total} onClick={() => setPage(page + 1)}>
              <Icon path={mdiArrowRightBold} size={1} />
            </ActionIcon>
          </Group>
        </>
      }
    >
      <Paper shadow="md" p="md" w="100%">
        <ScrollArea viewportRef={viewport} offsetScrollbars scrollbarSize={4} h="calc(100vh - 190px)">
          <Table className={tableClasses.table}>
            <Table.Thead>
              <Table.Tr>
                <Table.Th w="35vw" miw="400px">
                  {t('common.label.team')}
                </Table.Th>
                <Table.Th>{t('admin.label.teams.members')}</Table.Th>
                <Table.Th>{t('admin.label.teams.bio')}</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {teams &&
                teams.map((team) => {
                  const members = team.members?.sort((a, b) => (a.captain ? (b.captain ? 0 : -1) : 1))

                  return (
                    <Table.Tr key={team.id}>
                      <Table.Td>
                        <Group justify="space-between" gap={0} wrap="nowrap">
                          <Group justify="left" wrap="nowrap" w="calc(100% - 7rem)">
                            <Avatar alt="avatar" src={team.avatar} radius="xl">
                              {team.name?.slice(0, 1)}
                            </Avatar>
                            <Input
                              variant="unstyled"
                              value={team.name ?? 'team'}
                              readOnly
                              classNames={{
                                wrapper: misc.teamNameWrapper,
                                input: cx(misc.w100, misc.fwBold, misc.noUserSelect),
                              }}
                            />
                          </Group>
                          <Badge size="md" color={team.locked ? 'yellow' : 'gray'}>
                            {team.locked ? t('admin.content.teams.locked') : t('admin.content.teams.unlocked')}
                          </Badge>
                        </Group>
                      </Table.Td>
                      <Table.Td>
                        <Tooltip.Group openDelay={300} closeDelay={100}>
                          <Avatar.Group spacing="md">
                            {members &&
                              members.slice(0, 8).map((m) => (
                                <Tooltip key={m.id} label={m.userName} withArrow classNames={tooltipClasses}>
                                  <Avatar alt="avatar" radius="xl" src={m.avatar}>
                                    {m.userName?.slice(0, 1) ?? 'U'}
                                  </Avatar>
                                </Tooltip>
                              ))}
                            {members && members.length > 8 && (
                              <Tooltip
                                label={
                                  <Text>
                                    {members
                                      .slice(8)
                                      .map((o) => o.userName)
                                      .join(',')}
                                  </Text>
                                }
                                withArrow
                              >
                                <Avatar alt="avatar" radius="xl">
                                  +{members.length - 8}
                                </Avatar>
                              </Tooltip>
                            )}
                          </Avatar.Group>
                        </Tooltip.Group>
                      </Table.Td>
                      <Table.Td>
                        <Text lineClamp={1} truncate size="sm">
                          {team.bio ?? t('team.placeholder.bio')}
                        </Text>
                      </Table.Td>
                      <Table.Td align="right">
                        <Group wrap="nowrap" gap="sm" justify="right">
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
                            message={t('admin.content.teams.lock', {
                              name: team.name,
                              action: team.locked ? t('admin.button.teams.do_unlock') : t('admin.button.teams.do_lock'),
                            })}
                            disabled={disabled}
                            onClick={() => onToggleLock(team)}
                          />

                          <ActionIconWithConfirm
                            iconPath={mdiDeleteOutline}
                            color="alert"
                            message={t('admin.content.teams.delete', {
                              name: team.name,
                            })}
                            disabled={disabled}
                            onClick={() => onDelete(team)}
                          />
                        </Group>
                      </Table.Td>
                    </Table.Tr>
                  )
                })}
            </Table.Tbody>
          </Table>
        </ScrollArea>
        <TeamEditModal
          size="35%"
          title={t('admin.button.teams.edit')}
          team={activeTeam}
          opened={isEditModalOpen}
          onClose={() => setIsEditModalOpen(false)}
          mutateTeam={(team: TeamWithDetailedUserInfo) => {
            updateTeams(
              [team, ...(teams?.filter((n) => n.id !== team.id) ?? [])].sort((a, b) => (a.id! < b.id! ? -1 : 1))
            )
          }}
        />
      </Paper>
    </AdminPage>
  )
}

export default Teams
