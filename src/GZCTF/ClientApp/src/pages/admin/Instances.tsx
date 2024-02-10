import {
  Badge,
  Box,
  Code,
  Group,
  Paper,
  ScrollArea,
  Select,
  Stack,
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
import { FC, forwardRef, useEffect, useState } from 'react'
import { Trans, useTranslation } from 'react-i18next'
import { ActionIconWithConfirm } from '@Components/ActionIconWithConfirm'
import AdminPage from '@Components/admin/AdminPage'
import { showErrorNotification } from '@Utils/ApiErrorHandler'
import { useChallengeTagLabelMap, getProxyUrl } from '@Utils/Shared'
import { useTableStyles, useTooltipStyles } from '@Utils/ThemeOverride'
import api, { ChallengeModel, ChallengeTag, TeamModel } from '@Api'

type SelectTeamItemProps = TeamModel & React.ComponentPropsWithoutRef<'div'>
type SelectChallengeItemProps = ChallengeModel & React.ComponentPropsWithoutRef<'div'>

const SelectTeamItem = forwardRef<HTMLDivElement, SelectTeamItemProps>(
  ({ name, id, ...others }: SelectTeamItemProps, ref) => (
    <Stack ref={ref} {...others} spacing={0}>
      <Text lineClamp={1}>
        <Text span c="dimmed">
          {`#${id} `}
        </Text>
        {name}
      </Text>
    </Stack>
  )
)

const SelectChallengeItem = forwardRef<HTMLDivElement, SelectChallengeItemProps>(
  ({ title, id, tag, ...others }: SelectChallengeItemProps, ref) => {
    const challengeTagLabelMap = useChallengeTagLabelMap()
    const tagInfo = challengeTagLabelMap.get(tag ?? ChallengeTag.Misc)!
    const theme = useMantineTheme()

    return (
      <Group ref={ref} {...others} spacing="sm">
        <Icon color={theme.colors[tagInfo.color][4]} path={tagInfo.icon} size={1} />
        <Text lineClamp={1}>
          <Text span c="dimmed">
            {`#${id} `}
          </Text>
          {title}
        </Text>
      </Group>
    )
  }
)

const Instances: FC = () => {
  const { data: instances, mutate } = api.admin.useAdminInstances({
    refreshInterval: 30 * 1000, // refresh every 30 seconds
    revalidateOnFocus: false,
  })

  const [teams, setTeams] = useState<TeamModel[]>()
  const [challenge, setChallenge] = useState<ChallengeModel[]>()
  const [disabled, setDisabled] = useState(false)
  const { classes, theme } = useTableStyles()
  const clipBoard = useClipboard()
  const { classes: tooltipClasses } = useTooltipStyles()
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
          <Group w="60%" position="left" spacing="md">
            <Select
              w="48%"
              searchable
              clearable
              placeholder={t('admin.placeholder.instances.teams.select')}
              value={selectedTeamId}
              onChange={(id) => setSelectedTeamId(id)}
              icon={<Icon path={mdiAccountGroupOutline} size={1} />}
              itemComponent={SelectTeamItem}
              data={
                teams?.map((team) => ({ value: String(team.id), label: team.name, ...team })) ?? []
              }
              filter={(query, team) => team.name.includes(query) || team.value.includes(query)}
              nothingFound={t('admin.placeholder.instances.teams.not_found')}
            />
            <Select
              w="48%"
              searchable
              clearable
              placeholder={t('admin.placeholder.instances.challenges.select')}
              onChange={(id) => setSelectedChallengeId(id)}
              icon={<Icon path={mdiPuzzleOutline} size={1} />}
              itemComponent={SelectChallengeItem}
              data={
                challenge?.map((challenge) => ({
                  value: String(challenge.id),
                  label: challenge.title,
                  ...challenge,
                })) ?? []
              }
              filter={(query, challenge) =>
                challenge.title.includes(query) || challenge.value.includes(query)
              }
              nothingFound={t('admin.placeholder.instances.challenges.not_found')}
            />
          </Group>

          <Group position="right">
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
          <Table className={classes.table}>
            <thead>
              <tr>
                <th>{t('common.label.team')}</th>
                <th>{t('common.label.challenge')}</th>
                <th>{t('admin.label.instances.life_cycle')}</th>
                <th>{t('admin.label.instances.container_id')}</th>
                <th>{t('admin.label.instances.entry')}</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {filteredInstances &&
                filteredInstances.map((inst) => {
                  const color = challengeTagLabelMap.get(
                    inst.challenge?.tag ?? ChallengeTag.Misc
                  )!.color
                  return (
                    <tr key={inst.containerGuid}>
                      <td>
                        <Box w="100%" h="100%">
                          <Text truncate size="sm" fw="bold" lineClamp={1}>
                            {inst.team?.name}
                          </Text>
                        </Box>
                      </td>
                      <td>
                        <Box w="100%" h="100%">
                          <Text truncate size="sm" fw="bold" lineClamp={1}>
                            {inst.challenge?.title}
                          </Text>
                        </Box>
                      </td>
                      <td>
                        <Group noWrap spacing="xs">
                          <Badge size="xs" color={color} variant="dot">
                            {dayjs(inst.startedAt).format('MM/DD HH:mm')}
                          </Badge>
                          <Icon path={mdiChevronTripleRight} size={1} />
                          <Badge size="xs" color={color} variant="dot">
                            {dayjs(inst.expectStopAt).format('MM/DD HH:mm')}
                          </Badge>
                        </Group>
                      </td>
                      <td>
                        <Text size="sm" ff={theme.fontFamilyMonospace} lineClamp={1}>
                          <Tooltip
                            label={t('common.button.copy')}
                            withArrow
                            position="left"
                            classNames={tooltipClasses}
                          >
                            <Text
                              size="sm"
                              ff={theme.fontFamilyMonospace}
                              style={{
                                backgroundColor: 'transparent',
                                fontSize: theme.fontSizes.sm,
                                cursor: 'pointer',
                              }}
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
                      </td>
                      <td>
                        <Tooltip
                          label={t('common.button.copy')}
                          withArrow
                          position="left"
                          classNames={tooltipClasses}
                        >
                          <Text
                            size="sm"
                            color="dimmed"
                            ff={theme.fontFamilyMonospace}
                            style={{
                              backgroundColor: 'transparent',
                              fontSize: theme.fontSizes.sm,
                              cursor: 'pointer',
                            }}
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
                      </td>
                      <td align="right">
                        <Group noWrap spacing="sm" position="right">
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
                      </td>
                    </tr>
                  )
                })}
            </tbody>
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
