using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class CallingCorridorGate : MonoBehaviour
    {
        [SerializeField] private CallingCorridorDoor[] doors;
        [SerializeField] private Collider straightRouteBlocker;
        [SerializeField] private TMPro.TMP_Text completionMessage;
        [SerializeField] private Transform exitPlane;
        [SerializeField] private GameObject exitDoorHardware;
        [SerializeField] private CallingCorridorRoomEncounter[] rooms;
        [SerializeField] private CallingCorridorRoomEncounter exitRoom;
        private CallingCorridorSession session;
        private CallingCorridorDoor selectedDoor;
        private CallingCorridorRoomEncounter selectedRoom;
        public int Index => session != null ? session.Decision : 0;
        public bool IsAvailable => session != null && session.CanChoose(Index);
        public bool IsConfigured => doors != null && doors.Length == 6 &&
            straightRouteBlocker != null && exitPlane != null && exitDoorHardware != null &&
            rooms != null && rooms.Length == 6 && exitRoom != null && exitRoom.IsConfigured && HasValidDoors();

        private bool HasValidDoors()
        {
            for (int i = 0; i < doors.Length; i++)
                if (doors[i] == null || !doors[i].IsConfigured || rooms[i] == null || !rooms[i].IsConfigured) return false;
            return true;
        }

        public void Bind(CallingCorridorSession owner, int index)
        {
            session = owner;
            if (exitRoom != null && !exitRoom.Bind(this, null))
                Debug.LogError("CALLING_CORRIDOR_CONFIGURATION_INVALID: exit rabbit animation missing", exitRoom);
            if (doors == null) return;
            for (int i = 0; i < doors.Length; i++)
            {
                doors[i]?.Bind(this, CallingCorridorRules.DoorCode(index, i));
                if (rooms == null || i >= rooms.Length || rooms[i] == null) continue;
                if (!rooms[i].Bind(this, doors[i]))
                    Debug.LogError("CALLING_CORRIDOR_CONFIGURATION_INVALID: room animation missing", rooms[i]);
            }
        }

        public bool IsPlayer(Collider other) => session != null && session.IsPlayer(other);
        public bool Contains(CallingCorridorDoor door)
        {
            for (int i = 0; i < doors.Length; i++)
                if (doors[i] == door) return true;
            return false;
        }

        public bool TrySelectDoor(CallingCorridorDoor door)
        {
            if (session == null || selectedDoor != null || !IsAvailable) return false;
            int index = System.Array.IndexOf(doors, door);
            if (index < 0 || rooms[index].IsConsumed) return false;
            selectedDoor = door;
            selectedRoom = rooms[index];
            if (!session.TryChoose(this, door)) { selectedDoor = null; selectedRoom = null; return false; }
            door.Consume(session.PlayerTransform);
            return true;
        }

        public CallingCorridorRoomEncounter SelectedRoom => selectedRoom;
        public CallingCorridorRoomEncounter ExitRoom => exitRoom;

        public bool TryEnterExit(CallingCorridorRoomEncounter room) =>
            room == exitRoom && session != null && session.IsPlaying && Index == 7 &&
            !room.IsConsumed && session.TryVisitExit(room);

        public void Refresh(int current)
        {
            if (straightRouteBlocker == null || doors == null || rooms == null || rooms.Length != doors.Length) return;
            straightRouteBlocker.gameObject.SetActive(current < 7);
            if (exitDoorHardware != null) exitDoorHardware.SetActive(current < 7);
            RefreshExit(current);
            if (completionMessage != null) completionMessage.gameObject.SetActive(false);
            for (int i = 0; i < doors.Length; i++)
                RefreshRoom(i, current);
        }

        private void RefreshExit(int current)
        {
            if (exitRoom == null) return;
            if (current == 7 && !exitRoom.IsConsumed) exitRoom.Configure(true);
            else if (current < 7) exitRoom.ResetEncounter();
        }

        private void RefreshRoom(int index, int current)
        {
            if (doors[index] == null || rooms[index] == null || doors[index] == selectedDoor) return;
            if (current >= 7) { doors[index].ResetDoor(); rooms[index].ResetEncounter(); return; }
            doors[index].Bind(this, CallingCorridorRules.DoorCode(current, index));
            rooms[index].Configure(doors[index].Code == CallingCorridorRules.ExpectedCode(current));
        }

        public void ResetRooms()
        {
            selectedDoor = null;
            selectedRoom = null;
            if (exitRoom != null) exitRoom.ResetEncounter();
            if (doors != null)
                for (int i = 0; i < doors.Length; i++) doors[i]?.ResetDoor();
            if (rooms == null) return;
            for (int i = 0; i < rooms.Length; i++) rooms[i]?.ResetEncounter();
        }

        public void ShowCompletion()
        {
            if (completionMessage == null) return;
            completionMessage.text = "ARCHIVE CLEARED\nDEMO COMPLETE";
            completionMessage.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (session == null || selectedDoor == null || !session.IsPlaying) return;
            if (!selectedDoor.HasReturnedToHall) return;
            selectedDoor = null;
            selectedRoom = null;
            Refresh(Index);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (session == null || !IsPlayer(other)) return;
            if (Vector3.Dot(other.transform.position - exitPlane.position, exitPlane.forward) > 0.3f)
                session.TryFinish();
        }

        private void OnTriggerStay(Collider other) => OnTriggerEnter(other);
    }
}
