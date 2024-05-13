import {
  Badge,
  Box,
  Code,
  ComboboxItem,
  Group,
  Input,
  Paper,
  ScrollArea,
  Select,
  SelectProps,
  Table,
  Text,
  Tooltip,
  useMantineTheme,
} from '@mantine/core'
import { useClipboard } from '@mantine/hooks'
import { showNotification } from '@mantine/notifications'
import {
  mdiAccountGroupOutline,
  mdiCheck,
  mdiChevronTripleRight,
  mdiPackageVariantClosedRemove,
  mdiPuzzleOutline,
} from '@mdi/js'
import { Icon } from '@mdi/react'
import dayjs from 'dayjs'
import { FC, useEffect, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import AdminPage from '@Components/admin/AdminPage'
import { showErrorNotification } from '@Utils/ApiHelper'
import { useChallengeTagLabelMap, getProxyUrl } from '@Utils/Shared'
import api, { ChallengeModel, ChallengeTag, TeamModel } from '@Api'
import tableClasses from '@Styles/Table.module.css'
import tooltipClasses from '@Styles/Tooltip.module.css'

type SelectTeamItemProps = TeamModel & ComboboxItem
type SelectChallengeItemProps = ChallengeModel & ComboboxItem

const SelectTeamItem: SelectProps['renderOption'] = ({ option }) => {
  const { name, id, ...others } = option as SelectTeamItemProps

  return (
    <Group {...others} gap={0} wrap="nowrap">
      <Text fw={500} size="sm" lineClamp={1} style={{ wordBreak: 'break-all' }}>
        <Text span c="dimmed">
          {`#${id} `}
        </Text>
        {name}
      </Text>
    </Group>
  )
}

const SelectChallengeItem: SelectProps['renderOption'] = ({ option }) => {
  const { title, id, tag } = option as SelectChallengeItemProps
  const challengeTagLabelMap = useChallengeTagLabelMap()
  const tagInfo = challengeTagLabelMap.get(tag ?? ChallengeTag.Misc)!
  const theme = useMantineTheme()

  return (
    <Group wrap="nowrap" gap="sm">
      <Icon color={theme.colors[tagInfo.color][4]} path={tagInfo.icon} size={1} />
      <Text fw={500} size="sm" lineClamp={1} style={{ wordBreak: 'break-all' }}>
        <Text span c="dimmed">
          {`#${id} `}
        </Text>
        {title}
      </Text>
    </Group>
  )
}

const Instances: FC = () => {
  const { data: instances, mutate } = api.admin.useAdminInstances({
    refreshInterval: 30 * 1000, // refresh every 30 seconds
    revalidateOnFocus: false,
  })

  const [teams, setTeams] = useState<TeamModel[]>()
  const [challenge, setChallenge] = useState<ChallengeModel[]>()
  const [disabled, setDisabled] = useState(false)
  const clipBoard = useClipboard()
  const challengeTagLabelMap = useChallengeTagLabelMap()

  const { t } = useTranslation()

  useEffect(() => {
    if (instances) {
      const teams = [
        ...new Map(instances.data.map((instance) => [instance.team!.id, instance.team!])).values(),
      ]
      setTeams(teams)

      const challenges = [
        ...new Map(
          instances.data.map((instance) => [instance.challenge!.id, instance.challenge!])
        ).values(),
      ]
      setChallenge(challenges)
    }
  }, [instances])

  const [selectedTeamId, setSelectedTeamId] = useState<string | null>(null)
  const [selectedChallengeId, setSelectedChallengeId] = useState<string | null>(null)

  const [filteredInstances, setFilteredInstances] = useState(instances?.data)

  useEffect(() => {
    if (!instances) return

    let filtered = instances.data

    if (selectedTeamId) {
      filtered = filtered.filter((instance) => instance.team?.id === Number(selectedTeamId))
    }

    if (selectedChallengeId) {
      filtered = filtered.filter(
        (instance) => instance.challenge?.id === Number(selectedChallengeId)
      )
    }

    setFilteredInstances(filtered)
  }, [instances, selectedTeamId, selectedChallengeId])

  const onDelete = async (instanceGuid?: string) => {
    if (!instanceGuid) return

    try {
      setDisabled(true)
      await api.admin.adminDestroyInstance(instanceGuid)

      showNotification({
        color: 'teal',
        message: t('admin.notification.instances.destroyed'),
        icon: <Icon path={mdiCheck} size={1} />,
      })

      instances &&
        mutate({
          total: (instances.total ?? instances.length) - 1,
          length: instances.length - 1,
          data: instances.data.filter((instance) => instance.containerGuid !== instanceGuid),
        })
    } catch (e: any) {
      showErrorNotification(e, t)
    } finally {
      setDisabled(false)
    }
  }

  return (
    <AdminPage
      isLoading={!instances || !teams || !challenge}
      head={
        <>
          <Group w="60%" justify="left" gap="md">
            <Select
              w="48%"
              searchable
              clearable
              placeholder={t('admin.placeholder.instances.teams.select')}
              value={selectedTeamId}
              onChange={(id) => setSelectedTeamId(id)}
              leftSection={<Icon path={mdiAccountGroupOutline} size={1} />}
              nothingFoundMessage={t('admin.placeholder.instances.teams.not_found')}
              renderOption={SelectTeamItem}
              data={
                teams?.map(
                  (team) => ({ value: String(team.id), label: team.name, ...team }) as ComboboxItem
                ) ?? []
              }
            />
            <Select
              w="48%"
              searchable
              clearable
              placeholder={t('admin.placeholder.instances.challenges.select')}
              onChange={(id) => setSelectedChallengeId(id)}
              leftSection={<Icon path={mdiPuzzleOutline} size={1} />}
              nothingFoundMessage={t('admin.placeholder.instances.challenges.not_found')}
              renderOption={SelectChallengeItem}
              data={
                challenge?.map(
                  (challenge) =>
                    ({
                      value: String(challenge.id),
                      label: challenge.title,
                      ...challenge,
                    }) as ComboboxItem
                ) ?? []
              }
            />
          </Group>

          <Group justify="right">
            <Text fw="bold" size="sm">
              <Trans i18nKey="admin.content.instances.stats" values={{ count: instances?.length }}>
                _<Code>_</Code>_
              </Trans>
            </Text>
          </Group>
        </>
      }
    >
      <Paper shadow="md" p="xs" w="100%">
        <ScrollArea offsetScrollbars scrollbarSize={4} h="calc(100vh - 205px)">
          <Table className={tableClasses.table}>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>{t('common.label.team')}</Table.Th>
                <Table.Th>{t('common.label.challenge')}</Table.Th>
                <Table.Th>{t('admin.label.instances.life_cycle')}</Table.Th>
                <Table.Th>{t('admin.label.instances.container_id')}</Table.Th>
                <Table.Th>{t('admin.label.instances.entry')}</Table.Th>
                <Table.Th />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {filteredInstances &&
                filteredInstances.map((inst) => {
                  const color = challengeTagLabelMap.get(
                    inst.challenge?.tag ?? ChallengeTag.Misc
                  )!.color
                  return (
                    <Table.Tr key={inst.containerGuid}>
                      <Table.Td>
                        <Box w="100%" h="100%">
                          <Input
                            variant="unstyled"
                            value={inst.team?.name ?? 'Team'}
                            readOnly
                            sx={() => ({
                              input: {
                                userSelect: 'none',
                                lineHeight: 1,
                                fontWeight: 700,
                                height: '1.5rem',
                              },
                            })}
                          />
                        </Box>
                      </Table.Td>
                      <Table.Td>
                        <Box w="100%" h="100%">
                          <Input
                            variant="unstyled"
                            value={inst.challenge?.title ?? 'Challenge'}
                            readOnly
                            sx={() => ({
                              input: {
                                userSelect: 'none',
                                lineHeight: 1,
                                fontWeight: 700,
                                height: '1.5rem',
                              },
                            })}
                          />
                        </Box>
                      </Table.Td>
                      <Table.Td>
                        <Group wrap="nowrap" gap="xs">
                          <Badge size="xs" color={color} variant="dot">
                            {dayjs(inst.startedAt).format('MM/DD HH:mm')}
                          </Badge>
                          <Icon path={mdiChevronTripleRight} size={1} />
                          <Badge size="xs" color={color} variant="dot">
                            {dayjs(inst.expectStopAt).format('MM/DD HH:mm')}
                          </Badge>
                        </Group>
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" ff="monospace" lineClamp={1}>
                          <Tooltip
                            label={t('common.button.copy')}
                            withArrow
                            position="left"
                            classNames={tooltipClasses}
                          >
                            <Text
                              size="sm"
                              ff="monospace"
                              bg="transparent"
                              fz="sm"
                              className={tableClasses.clickable}
                              onClick={() => {
                                clipBoard.copy(
                                  inst.containerGuid && getProxyUrl(inst.containerGuid)
                                )
                                showNotification({
                                  color: 'teal',
                                  title: t('admin.notification.instances.url_copied.title'),
                                  message: t('admin.notification.instances.url_copied.message'),
                                  icon: <Icon path={mdiCheck} size={1} />,
                                })
                              }}
                            >
                              {inst.containerGuid}
                            </Text>
                          </Tooltip>
                        </Text>
                      </Table.Td>
                      <Table.Td>
                        <Tooltip
                          label={t('common.button.copy')}
                          withArrow
                          position="left"
                          classNames={tooltipClasses}
                        >
                          <Text
                            size="sm"
                            c="dimmed"
                            ff="monospace"
                            bg="transparent"
                            fz="sm"
                            className={tableClasses.clickable}
                            onClick={() => {
                              clipBoard.copy(`${inst.ip ?? ''}:${inst.port ?? ''}`)
                              showNotification({
                                color: 'teal',
                                message: t('admin.notification.instances.entry_copied'),
                                icon: <Icon path={mdiCheck} size={1} />,
                              })
                            }}
                          >
                            {`${inst.ip}:`}
                            <Text span fw="bold" c="white">
                              {inst.port}
                            </Text>
                          </Text>
                        </Tooltip>
                      </Table.Td>
                      <Table.Td align="right">
                        <Group wrap="nowrap" gap="sm" justify="right">
                          <ActionIconWithConfirm
                            iconPath={mdiPackageVariantClosedRemove}
                            color="alert"
                            message={t('admin.content.instances.destroy', {
                              name: inst.containerGuid?.slice(0, 8),
                            })}
                            disabled={disabled}
                            onClick={() => onDelete(inst.containerGuid)}
                          />
                        </Group>
                      </Table.Td>
                    </Table.Tr>
                  )
                })}
            </Table.Tbody>
          </Table>
        </ScrollArea>
        <Text size="xs" c="dimmed">
          {t('admin.content.instances.note')}
        </Text>
      </Paper>
    </AdminPage>
  )
}

export default Instances
