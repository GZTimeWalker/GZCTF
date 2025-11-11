import {
  Accordion,
  ActionIcon,
  Button,
  Drawer,
  DrawerProps,
  Group,
  Input,
  MultiSelect,
  ScrollArea,
  Stack,
  Text,
  TextInput,
} from '@mantine/core'
import { showNotification } from '@mantine/notifications'
import { mdiCheck, mdiClose, mdiDiceMultiple, mdiMinusCircle } from '@mdi/js'
import { Icon } from '@mdi/react'
import { FC, useEffect, useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ScrollingText } from '@Components/ScrollingText'
import { PermissionDot, PermissionSelector } from '@Components/admin/PermissionSelector'
import { PERMISSION_DEFINITIONS, permissionMaskToArray } from '@Utils/Permission'
import { randomInviteCode, showErrorMsg } from '@Utils/Shared'
import { ChallengeInfoModel, Division, DivisionCreateModel, GamePermission } from '@Api'
import api from '@Api'

interface DivisionEditDrawerProps extends DrawerProps {
  gameId: number
  division?: Division | null
  challenges: ChallengeInfoModel[] | null
  onDivisionSaved: (division: Division) => void
}

type ChallengePermissionState = Record<number, number>

export const DivisionEditDrawer: FC<DivisionEditDrawerProps> = ({
  gameId,
  division,
  challenges,
  onDivisionSaved,
  ...drawerProps
}) => {
  const { t } = useTranslation()

  const {
    opened,
    onClose,
    title,
    size,
    position,
    closeOnClickOutside,
    overlayProps: incomingOverlayProps,
    ...restDrawerProps
  } = drawerProps

  const overlayProps = { blur: 3, backgroundOpacity: 0.55, ...(incomingOverlayProps ?? {}) }

  const [name, setName] = useState('')
  const [inviteCode, setInviteCode] = useState('')
  const [defaultPermissions, setDefaultPermissions] = useState<number>(GamePermission.All)
  const [selectedChallenges, setSelectedChallenges] = useState<string[]>([])
  const [challengePermissions, setChallengePermissions] = useState<ChallengePermissionState>({})
  const [loading, setLoading] = useState(false)

  const challengeMap = useMemo(() => {
    const map = new Map<number, ChallengeInfoModel>()
    challenges?.forEach((challenge) => {
      if (challenge.id !== undefined && challenge.id !== null) {
        map.set(challenge.id, challenge)
      }
    })
    return map
  }, [challenges])

  const challengeOptions = useMemo(() => {
    const options = (challenges ?? [])
      .filter((challenge) => challenge.id !== undefined && challenge.id !== null)
      .map((challenge) => ({
        value: challenge.id!.toString(),
        label: challenge.title ?? `${t('common.label.challenge')} #${challenge.id}`,
      }))

    selectedChallenges.forEach((value) => {
      if (!options.find((option) => option.value === value)) {
        options.push({
          value,
          label: t('admin.content.games.divisions.unknown_challenge', { id: Number(value) }),
        })
      }
    })

    return options.sort((a, b) => a.label.localeCompare(b.label))
  }, [challenges, selectedChallenges, t])

  const sortedSelected = useMemo(() => {
    return [...selectedChallenges].sort((a, b) => {
      const left = challengeMap.get(Number(a))?.title ?? `#${a}`
      const right = challengeMap.get(Number(b))?.title ?? `#${b}`
      return left.localeCompare(right)
    })
  }, [challengeMap, selectedChallenges])

  const resetForm = () => {
    setName(division?.name ?? '')
    setInviteCode(division?.inviteCode ?? '')
    setDefaultPermissions(division?.defaultPermissions ?? GamePermission.All)

    const configs = division?.challengeConfigs ?? []
    const overrides: ChallengePermissionState = {}
    const ids = configs.map((config) => {
      overrides[config.challengeId] = config.permissions ?? GamePermission.All
      return config.challengeId.toString()
    })

    setSelectedChallenges(ids)
    setChallengePermissions(overrides)
  }

  useEffect(() => {
    if (opened) {
      resetForm()
      setLoading(false)
    }
  }, [division, opened])

  const handleChallengeSelection = (values: string[]) => {
    setSelectedChallenges(values)
    setChallengePermissions((prev) => {
      const next: ChallengePermissionState = { ...prev }

      Object.keys(next).forEach((key) => {
        if (!values.includes(key)) {
          delete next[Number(key)]
        }
      })

      values.forEach((value) => {
        const id = Number(value)
        if (!(id in next)) {
          next[id] = defaultPermissions
        }
      })

      return next
    })
  }

  const handleOverrideChange = (challengeId: number, permissions: number) => {
    setChallengePermissions((prev) => ({ ...prev, [challengeId]: permissions }))
  }

  const getChallengeTitle = (id: number) =>
    challengeMap.get(id)?.title ?? t('admin.content.games.divisions.unknown_challenge', { id })

  const buildModel = (trimmedName: string): DivisionCreateModel => ({
    name: trimmedName,
    inviteCode: inviteCode.trim() ? inviteCode.trim() : null,
    defaultPermissions,
    challengeConfigs: selectedChallenges.map((value) => {
      const id = Number(value)
      return {
        challengeId: id,
        permissions: challengePermissions[id] ?? defaultPermissions,
      }
    }),
  })

  const handleSubmit = async () => {
    const trimmedName = name.trim()
    if (!trimmedName) {
      showNotification({
        color: 'red',
        message: t('common.error.empty'),
        icon: <Icon path={mdiClose} size={1} />,
      })
      return
    }

    setLoading(true)

    const model = buildModel(trimmedName)

    try {
      if (division) {
        const response = await api.edit.editUpdateDivision(gameId, division.id, model)

        showNotification({
          color: 'teal',
          message: t('admin.notification.games.divisions.updated'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        onDivisionSaved({ ...response.data, challengeConfigs: response.data.challengeConfigs ?? [] })
      } else {
        const created = await api.edit.editCreateDivision(gameId, model)

        showNotification({
          color: 'teal',
          message: t('admin.notification.games.divisions.created'),
          icon: <Icon path={mdiCheck} size={1} />,
        })
        onDivisionSaved({ ...created.data, challengeConfigs: created.data.challengeConfigs ?? [] })
      }

      onClose?.()
    } catch (error) {
      showErrorMsg(error, t)
    } finally {
      setLoading(false)
    }
  }

  const renderPermissionDots = (mask?: number | null, includeGlobal = false) => {
    const grantedValues = new Set(permissionMaskToArray(mask))
    const allDefinitions = PERMISSION_DEFINITIONS.filter((def) => includeGlobal || def.challengeScoped)

    return (
      <Group gap={6} wrap="wrap">
        {allDefinitions.map((definition) => (
          <PermissionDot key={definition.value} {...definition} granted={grantedValues.has(definition.value)} />
        ))}
      </Group>
    )
  }

  return (
    <Drawer
      {...restDrawerProps}
      opened={opened}
      title={title}
      size={size ?? 'xl'}
      position={position ?? 'right'}
      closeOnClickOutside={closeOnClickOutside ?? !loading}
      onClose={() => !loading && onClose?.()}
      overlayProps={overlayProps}
    >
      <Stack gap="sm">
        <Group gap="sm" grow>
          <TextInput
            label={t('admin.content.games.divisions.form.name.label')}
            description={t('admin.content.games.divisions.form.name.description')}
            placeholder={t('admin.placeholder.games.divisions')}
            withAsterisk
            disabled={loading}
            value={name}
            onChange={(event) => setName(event.currentTarget.value)}
          />
          <TextInput
            label={t('admin.content.games.divisions.form.invite_code.label')}
            description={t('admin.content.games.divisions.form.invite_code.description')}
            placeholder={t('admin.content.games.info.invite_code.placeholder')}
            value={inviteCode}
            disabled={loading}
            onChange={(event) => setInviteCode(event.currentTarget.value)}
            rightSection={
              <ActionIcon disabled={loading} onClick={() => !loading && setInviteCode(randomInviteCode())}>
                <Icon path={mdiDiceMultiple} size={0.9} />
              </ActionIcon>
            }
          />
        </Group>

        <Input.Wrapper
          label={t('admin.content.games.divisions.form.default_permissions.label')}
          description={t('admin.content.games.divisions.form.default_permissions.description')}
        >
          <PermissionSelector pt="md" value={defaultPermissions} onChange={setDefaultPermissions} disabled={loading} />
        </Input.Wrapper>

        <MultiSelect
          label={t('admin.content.games.divisions.form.challenge_overrides.label')}
          description={t('admin.content.games.divisions.form.challenge_overrides.description')}
          data={challengeOptions}
          value={selectedChallenges}
          onChange={handleChallengeSelection}
          searchable
          disabled={loading || !challenges}
          nothingFoundMessage={t('admin.content.nothing_found')}
          placeholder={t('admin.content.games.divisions.form.challenge_overrides.placeholder')}
        />

        {sortedSelected.length > 0 && (
          <ScrollArea type="auto" offsetScrollbars h={300}>
            <Accordion chevronPosition="left" variant="filled" radius="md">
              {sortedSelected.map((value) => {
                const id = Number(value)
                return (
                  <Accordion.Item value={value} key={value}>
                    <Accordion.Control>
                      <Group justify="space-between" wrap="nowrap">
                        <Group gap="xs">
                          <Text size="sm" miw="2rem">{`#${id}`}</Text>
                          <ScrollingText text={getChallengeTitle(id)} w="12rem" />
                          {renderPermissionDots(challengePermissions[id])}
                        </Group>
                        <Button
                          variant="subtle"
                          size="xs"
                          color="red"
                          disabled={loading}
                          leftSection={<Icon path={mdiMinusCircle} size={0.8} />}
                          onClick={(event) => {
                            event.stopPropagation()
                            handleChallengeSelection(selectedChallenges.filter((item) => item !== value))
                          }}
                        >
                          {t('common.modal.delete')}
                        </Button>
                      </Group>
                    </Accordion.Control>
                    <Accordion.Panel>
                      <PermissionSelector
                        challengeScoped
                        value={challengePermissions[id] ?? defaultPermissions}
                        onChange={(permissions) => handleOverrideChange(id, permissions)}
                        disabled={loading}
                      />
                    </Accordion.Panel>
                  </Accordion.Item>
                )
              })}
            </Accordion>
          </ScrollArea>
        )}

        <Group justify="flex-end">
          <Button variant="default" disabled={loading} onClick={onClose}>
            {t('common.modal.cancel')}
          </Button>
          <Button onClick={handleSubmit} loading={loading}>
            {division ? t('common.modal.confirm_update') : t('common.modal.confirm')}
          </Button>
        </Group>
      </Stack>
    </Drawer>
  )
}
